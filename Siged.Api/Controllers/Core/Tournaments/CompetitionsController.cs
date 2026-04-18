using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.DTOs.Tournaments.Discipline; 
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;
using Microsoft.AspNetCore.Authorization;
using Siged.Domain.Entities.Security;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bloqueado por defecto para seguridad SIGED
    public class CompetitionsController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly TournamentManagerService _tournamentService;

        public CompetitionsController(ApplicationDbContext context, TournamentManagerService tournamentService)
        {
            _context = context;
            _tournamentService = tournamentService;
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
                .Include(c => c.CompetitionTeams) // 👈 Cargamos la lista de inscripciones
                    .ThenInclude(i => i.Team)   // 👈 Y de cada inscripción, cargamos el Team
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comp == null) return NotFound();
            return Ok(comp);
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
