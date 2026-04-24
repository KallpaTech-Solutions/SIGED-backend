using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.DTOs.Tournaments.Bracket;
using Siged.Application.DTOs.Tournaments.Discipline;
using Siged.Application.DTOs.Tournaments.Standing;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;
using Microsoft.AspNetCore.Authorization;
using Siged.Api.Authorization;
using Siged.Domain.Entities.Security;
using Siged.Api.Services;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bloqueado por defecto para seguridad SIGED
    public class CompetitionsController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly TournamentManagerService _tournamentService;
        private readonly StandingsService _standingsService;
        private readonly BracketService _bracketService;
        private readonly CompetitionFormatSetupService _formatSetupService;
        private readonly TournamentVitrinaBroadcastService _vitrina;

        public CompetitionsController(
            ApplicationDbContext context,
            TournamentManagerService tournamentService,
            StandingsService standingsService,
            BracketService bracketService,
            CompetitionFormatSetupService formatSetupService,
            TournamentVitrinaBroadcastService vitrina)
        {
            _context = context;
            _tournamentService = tournamentService;
            _standingsService = standingsService;
            _bracketService = bracketService;
            _formatSetupService = formatSetupService;
            _vitrina = vitrina;
        }

        /// <summary>
        /// Crea una nueva competición y clona las reglas de la disciplina seleccionada.
        /// </summary>
        /// <param name="dto">Datos de la competición a crear.</param>
        /// <returns>La competición creada.</returns>
        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create(CreateCompetitionDto dto)
        {
            // 🛡️ Iniciamos una transacción para asegurar que no haya datos huérfanos
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var competition = new Competition
                {
                    TournamentId = dto.TournamentId,
                    DisciplineId = dto.DisciplineId,
                    Gender = dto.Gender,
                    CategoryName = dto.CategoryName,
                    IsActive = true
                };

                _context.Competitions.Add(competition);
                await _context.SaveChangesAsync();

                // 🚀 Clonación de reglas desde la Disciplina
                await _tournamentService.CloneRulesToCompetition(competition.Id, dto.DisciplineId);

                await transaction.CommitAsync();
                return CreatedAtAction(nameof(GetById), new { id = competition.Id }, competition);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error al crear la competición y sus reglas.");
            }
        }

        /// <summary>
        /// Armado automático del formato: grupos equilibrados (ej. 15 equipos, máx. 4 → 4+4+4+3) con tablas RR,
        /// o eliminación directa. Solo si la competencia aún no tiene fases. Requiere equipos ya inscritos.
        /// </summary>
        [HttpPost("{id}/setup-format")]
        [Authorize(Policy = TournFormatSetupAuth.PolicyName)]
        public async Task<IActionResult> SetupFormat(Guid id, [FromBody] SetupCompetitionFormatDto dto)
        {
            try
            {
                var result = await _formatSetupService.SetupAsync(id, dto);
                await _vitrina.NotifyLandingRefreshAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "No se pudo configurar el formato.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene una competición por su ID, incluyendo su disciplina y torneo relacionados.
        /// </summary>
        /// <param name="id">ID de la competición.</param>
        /// <returns>La competición encontrada o NotFound si no existe.</returns>
        [HttpGet("{id}")]
        [AllowAnonymous] // Permitimos acceso público para consulta de competiciones
        public async Task<IActionResult> GetById(Guid id)
        {
            var comp = await _context.Competitions
                .Include(c => c.Tournament)
                .Include(c => c.Discipline)
                .Include(c => c.CompetitionTeams)
                    .ThenInclude(i => i.Team)
                        .ThenInclude(tm => tm.Organizacion)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comp == null) return NotFound();

            var formatSetup = await _context.CompetitionRules
                .AsNoTracking()
                .Where(r => r.CompetitionId == id && r.RuleKey.StartsWith("FORMAT_SETUP_"))
                .ToDictionaryAsync(r => r.RuleKey, r => r.RuleValue ?? "");
            comp.FormatSetup = formatSetup.Count > 0 ? formatSetup : null;

            return Ok(comp);
        }

        /// <summary>
        /// Vitrina pública: fases (modalidad), tablas por grupo (round robin), llaves (eliminación) y partidos.
        /// </summary>
        [HttpGet("{id}/public-dashboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicDashboard(Guid id)
        {
            var comp = await _context.Competitions
                .AsNoTracking()
                .Include(c => c.Phases)
                .Include(c => c.Tournament)
                .Include(c => c.Discipline)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comp == null) return NotFound();

            var phases = comp.Phases.OrderBy(p => p.Order).ToList();
            var phasesOut = new List<object>();

            foreach (var phase in phases)
            {
                var phaseHasGroups = await _context.Groups.AnyAsync(g => g.PhaseId == phase.Id);
                var useRoundRobinLayout = phase.Type == PhaseType.RoundRobin
                    || (phase.Type == PhaseType.Suizo && phaseHasGroups);

                if (useRoundRobinLayout)
                {
                    var groups = await _context.Groups
                        .AsNoTracking()
                        .Where(g => g.PhaseId == phase.Id)
                        .OrderBy(g => g.Name)
                        .ToListAsync();

                    var groupsPayload = new List<object>();
                    foreach (var g in groups)
                    {
                        List<TeamStandingDto> standings =
                            await _standingsService.GetStandingsByGroupAsync(g.Id);

                        var matches = await LoadMatchRowsForGroupAsync(g.Id);

                        groupsPayload.Add(new
                        {
                            g.Id,
                            g.Name,
                            g.QualifiedCount,
                            Standings = standings,
                            Matches = matches
                        });
                    }

                    phasesOut.Add(new
                    {
                        phase.Id,
                        phase.Name,
                        Type = phase.Type.ToString(),
                        phase.Order,
                        phase.IsDoubleLeg,
                        phase.IsDirectElimination,
                        Mode = "roundRobin",
                        Groups = groupsPayload
                    });
                }
                else
                {
                    BracketDto bracket = await _bracketService.GetBracketByPhaseAsync(phase.Id);

                    var matches = await LoadMatchRowsForPhaseAsync(phase.Id);

                    phasesOut.Add(new
                    {
                        phase.Id,
                        phase.Name,
                        Type = phase.Type.ToString(),
                        phase.Order,
                        phase.IsDoubleLeg,
                        phase.IsDirectElimination,
                        Mode = "knockout",
                        Bracket = bracket,
                        Matches = matches
                    });
                }
            }

            var statusCounts = await _context.Matches
                .AsNoTracking()
                .Where(m => m.Phase.CompetitionId == id && m.IsActive)
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int count(MatchStatus s) => statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

            return Ok(new
            {
                CompetitionId = comp.Id,
                TournamentId = comp.TournamentId,
                TournamentName = comp.Tournament.Name,
                TournamentYear = comp.Tournament.Year,
                DisciplineName = comp.Discipline.Name,
                CategoryName = comp.CategoryName,
                Gender = comp.Gender.ToString(),
                Phases = phasesOut,
                MatchSummary = new
                {
                    Total = count(MatchStatus.Programado) + count(MatchStatus.EnVivo) + count(MatchStatus.Finalizado) + count(MatchStatus.Suspendido),
                    Programado = count(MatchStatus.Programado),
                    EnVivo = count(MatchStatus.EnVivo),
                    Finalizado = count(MatchStatus.Finalizado),
                    Suspendido = count(MatchStatus.Suspendido)
                }
            });
        }

        /// <summary>
        /// Proyección en SQL (join a Venue) para que <c>VenueName</c> no dependa del estado de navegaciones tras Include.
        /// </summary>
        private async Task<List<object>> LoadMatchRowsForGroupAsync(Guid groupId)
        {
            var rows = await _context.Matches
                .AsNoTracking()
                .Where(m => m.GroupId == groupId && m.IsActive)
                .OrderBy(m => m.ScheduledAt)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.PhaseId,
                    m.GroupId,
                    m.Status,
                    m.ScheduledAt,
                    m.LocalScore,
                    m.VisitorScore,
                    VenueName = m.Venue != null ? m.Venue.Name : null,
                    LN = m.LocalTeam != null ? m.LocalTeam.Name : null,
                    VN = m.VisitorTeam != null ? m.VisitorTeam.Name : null,
                    LLogo = m.LocalTeam != null ? m.LocalTeam.LogoUrl : null,
                    VLogo = m.VisitorTeam != null ? m.VisitorTeam.LogoUrl : null,
                })
                .ToListAsync();

            return rows.Select(r => (object)new
            {
                r.Id,
                r.PhaseId,
                r.GroupId,
                Status = r.Status.ToString(),
                r.ScheduledAt,
                r.LocalScore,
                r.VisitorScore,
                r.VenueName,
                LocalTeamName = r.LN ?? (r.Status == MatchStatus.Finalizado ? "—" : "Por definir"),
                VisitorTeamName = r.VN ?? (r.Status == MatchStatus.Finalizado ? "—" : "Por definir"),
                LocalTeamLogo = r.LLogo,
                VisitorTeamLogo = r.VLogo,
            }).ToList();
        }

        private async Task<List<object>> LoadMatchRowsForPhaseAsync(Guid phaseId)
        {
            var rows = await _context.Matches
                .AsNoTracking()
                .Where(m => m.PhaseId == phaseId && m.IsActive)
                .OrderBy(m => m.ScheduledAt)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.PhaseId,
                    m.GroupId,
                    m.Status,
                    m.ScheduledAt,
                    m.LocalScore,
                    m.VisitorScore,
                    VenueName = m.Venue != null ? m.Venue.Name : null,
                    LN = m.LocalTeam != null ? m.LocalTeam.Name : null,
                    VN = m.VisitorTeam != null ? m.VisitorTeam.Name : null,
                    LLogo = m.LocalTeam != null ? m.LocalTeam.LogoUrl : null,
                    VLogo = m.VisitorTeam != null ? m.VisitorTeam.LogoUrl : null,
                })
                .ToListAsync();

            return rows.Select(r => (object)new
            {
                r.Id,
                r.PhaseId,
                r.GroupId,
                Status = r.Status.ToString(),
                r.ScheduledAt,
                r.LocalScore,
                r.VisitorScore,
                r.VenueName,
                LocalTeamName = r.LN ?? (r.Status == MatchStatus.Finalizado ? "—" : "Por definir"),
                VisitorTeamName = r.VN ?? (r.Status == MatchStatus.Finalizado ? "—" : "Por definir"),
                LocalTeamLogo = r.LLogo,
                VisitorTeamLogo = r.VLogo,
            }).ToList();
        }

        /// <summary>
        /// Obtiene todas las competiciones de un torneo específico, incluyendo su disciplina relacionada.
        /// </summary>
        /// <param name="tournamentId">ID del torneo.</param>
        /// <returns>Lista de competiciones del torneo.</returns>
        [HttpGet("tournament/{tournamentId}")]
        [AllowAnonymous] // Permitimos acceso público para consulta de competiciones por torneo
        public async Task<IActionResult> GetByTournament(Guid tournamentId)
        {
            var competitions = await _context.Competitions
                .Include(c => c.Discipline)
                .Where(c => c.TournamentId == tournamentId)
                .ToListAsync();

            return Ok(competitions);
        }
        // 1. EDITAR: Cambiar datos (Deporte, Género, Categoría)
        // Modifica tu Update para que no cree duplicados por error
        /// <summary>
        /// Actualiza los datos de una competición existente, validando que no se creen duplicados por error.
        /// </summary>
        /// <param name="id">ID de la competición a actualizar.</param>
        /// <param name="dto">Datos de la competición a actualizar.</param>
        /// <returns>La competición actualizada o un error si existe un duplicado. </returns>
        [HttpPut("{id}")]
        [Authorize(Policy =Permissions.TournManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateCompetitionDto dto)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null) return NotFound();

            // Validar que el cambio no choque con otra competencia existente
            var duplicate = await _context.Competitions.AnyAsync(c =>
                c.Id != id &&
                c.TournamentId == competition.TournamentId &&
                c.DisciplineId == dto.DisciplineId &&
                c.Gender == dto.Gender);

            if (duplicate) return BadRequest("Ya existe otra competición con ese Deporte y Género en este torneo.");

            competition.DisciplineId = dto.DisciplineId;
            competition.Gender = dto.Gender;
            competition.CategoryName = dto.CategoryName;

            await _context.SaveChangesAsync();
            return Ok(competition);
        }

        // 2. CAMBIAR ESTADO: Activar o Desactivar (Soft Delete/Restore)
        /// <summary>
        /// Alterna el estado de una competición entre activa e inactiva. Esto permite "eliminar" sin perder datos históricos, y también restaurar si se desactiva por error.  
        /// </summary>
        /// <param name="id">ID de la competición.</param>
        /// <returns>La competición con su nuevo estado.</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null) return NotFound();

            competition.IsActive = !competition.IsActive; // Si es true pasa a false, y viceversa
            await _context.SaveChangesAsync();

            return Ok(new { id = competition.Id, isActive = competition.IsActive });
        }

        // 3. ELIMINAR FÍSICAMENTE: Solo para errores de creación (Hard Delete)
        /// <summary>
        /// Elimina físicamente una competición solo si no tiene datos relacionados (fases, grupos, etc.). Esto es útil para corregir errores de creación sin dejar datos huérfanos. Si la competición ya tiene fases configuradas, se recomienda usar la desactivación en su lugar para mantener la integridad histórica.
        /// 
        /// </summary>
        /// <param name="id">ID de la competición a eliminar.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.SecurityRoleManage)]   
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var competition = await _context.Competitions
                .Include(c => c.Phases) // Cargamos las fases para verificar
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition == null) return NotFound();

            // 🛡️ REGLA DE ORO DE INGENIERÍA:
            // No permitas borrar si ya tiene datos relacionados (fases, grupos, etc.)
            if (competition.Phases.Any())
            {
                return BadRequest("No se puede eliminar físicamente porque ya tiene fases configuradas. Use la desactivación en su lugar.");
            }

            _context.Competitions.Remove(competition);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        /// <summary>
        /// Obtiene las reglas específicas de una competición. Accesible para todo el público 
        /// para garantizar la transparencia del reglamento del torneo.
        /// </summary>
        /// <param name="id">ID de la competición.</param>
        /// <response code="200">Devuelve la lista de reglas (puntos, duración, etc.).</response>
        /// <response code="404">Si la competición no existe.</response>
        [HttpGet("{id}/rules")]
        [AllowAnonymous] // Permitimos acceso público para consulta de reglas, ya que son necesarias para la configuración de fases y grupos en el Frontend
        public async Task<IActionResult> GetRules(Guid id)
        {
            var rules = await _context.CompetitionRules
                .Where(r => r.CompetitionId == id)
                .ToListAsync();
            return Ok(rules);
        }

        /// <summary>
        /// Actualiza un valor de regla específico para esta competición.
        /// </summary>
        [HttpPut("{id}/rules/{ruleKey}")]
        [Authorize(Policy = Permissions.TournManage)] // Solo para gestores de torneos, ya que son reglas internas
        public async Task<IActionResult> UpdateRule(Guid id, string ruleKey, [FromBody] string newValue)
        {
            var rule = await _context.CompetitionRules
                .FirstOrDefaultAsync(r => r.CompetitionId == id && r.RuleKey == ruleKey);

            if (rule == null) return NotFound();

            rule.RuleValue = newValue;
            await _context.SaveChangesAsync();
            return Ok(rule);
        }

        /// <summary>
        /// Actualiza masivamente las reglas de una competición.
        /// Solo accesible por gestores de torneos.
        /// </summary>
        /// <response code="200">Reglas actualizadas exitosamente.</response>
        /// <response code="400">Si los datos del DTO son inválidos.</response>
        /// <response code="404">Si no se encuentran reglas para esa competición.</response>
        [HttpPut("{id}/rules-bulk")]
        [Authorize(Policy = Permissions.TournManage)] // Solo para gestores de torneos, ya que son reglas internas
        public async Task<IActionResult> UpdateRulesBulk(Guid id, [FromBody] List<DisciplineRuleDto> rulesDto)
        {
            var rules = await _context.CompetitionRules
                .Where(r => r.CompetitionId == id)
                .ToListAsync();

            if (!rules.Any()) return NotFound("No se encontraron reglas para esta competición.");

            foreach (var dto in rulesDto)
            {
                var rule = rules.FirstOrDefault(r => r.RuleKey == dto.RuleKey);
                if (rule != null) rule.RuleValue = dto.RuleValue;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Reglas actualizadas correctamente." });
        }
    }
}
