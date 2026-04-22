using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Authorization;
using Siged.Application.DTOs.Tournaments.Player;
using Siged.Application.DTOs.Tournaments.Team;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bloqueado por defecto para seguridad SIGED
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public TeamsController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        /// <summary>
        /// Retrieves all teams, optionally filtering by active status.
        /// </summary>
        /// <param name="onlyActive">true to include only active teams; false to include all teams.</param>
        /// <returns>An IActionResult containing a list of TeamDto objects.</returns>
        /// <response code="200">Returns the list of teams.</response>
        /// <response code="401">Unauthorized access.</response>
        /// <response code="500">Internal server error.</response>
        /// <response code="400">Bad request.</response>
        /// <response code="404">Not found.</response>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        {
            var query = _context.Teams
                .Include(t => t.Organizacion)
                .AsQueryable();

            if (onlyActive) query = query.Where(t => t.IsActive);

            var teams = await query.OrderBy(t => t.Name).Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Initials = t.Initials,
                LogoUrl = t.LogoUrl,
                RepresentativeName = t.RepresentativeName,
                IsActive = t.IsActive,
                // ✅ Ahora el compilador encontrará estas propiedades:
                NombreEscuela = t.Organizacion.Nombre,
                ColorEscuela = t.Organizacion.ColorRepresentativo
            }).ToListAsync();

            return Ok(teams);
        }

        /// <summary>
        /// Delegados: escuela vinculada al usuario y equipos activos (para inscripción).
        /// </summary>
        [HttpGet("me/context")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> GetMyContext()
        {
            var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
            if (orgId == null)
                return Ok(new { organizacionId = (int?)null, nombreEscuela = (string?)null, teams = Array.Empty<object>() });

            var org = await _context.Organizaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);
            var teams = await _context.Teams
                .AsNoTracking()
                .Where(t => t.OrganizacionId == orgId.Value && t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.Initials })
                .ToListAsync();

            return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams });
        }

        /// <summary>
        /// Panel delegado: equipos de la escuela, inscripciones por competencia/torneo y planteles (activos e inactivos).
        /// </summary>
        [HttpGet("me/summary")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> GetMyDelegateSummary()
        {
            var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
            if (orgId == null)
                return Ok(new { organizacionId = (int?)null, nombreEscuela = (string?)null, teams = Array.Empty<object>() });

            var org = await _context.Organizaciones.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);

            var teamList = await _context.Teams
                .AsNoTracking()
                .Where(t => t.OrganizacionId == orgId.Value && t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.Initials })
                .ToListAsync();

            var teamIds = teamList.Select(t => t.Id).ToList();
            if (teamIds.Count == 0)
                return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams = Array.Empty<object>() });

            var ctRows = await _context.CompetitionTeams
                .AsNoTracking()
                .Where(ct => teamIds.Contains(ct.TeamId))
                .Select(ct => new
                {
                    ct.TeamId,
                    ct.CompetitionId,
                    TournamentId = ct.Competition.TournamentId,
                    TournamentName = ct.Competition.Tournament.Name,
                    DisciplineName = ct.Competition.Discipline.Name,
                    ct.Competition.CategoryName,
                    Gender = ct.Competition.Gender
                })
                .ToListAsync();

            var playersRaw = await _context.Players
                .AsNoTracking()
                .Where(p => teamIds.Contains(p.TeamId))
                .OrderBy(p => p.Number)
                .ThenBy(p => p.Name)
                .ToListAsync();

            static PlayerDto MapPlayer(Player p) => new()
            {
                Id = p.Id,
                TeamId = p.TeamId,
                Name = p.Name,
                Dni = p.Dni,
                BirthDate = p.BirthDate,
                Number = p.Number,
                Position = p.Position,
                PhotoUrl = p.PhotoUrl,
                IsActive = p.IsActive,
                IsEligible = p.IsEligible
            };

            var teamsOut = teamList.Select(team =>
            {
                var inscriptions = ctRows
                    .Where(x => x.TeamId == team.Id)
                    .GroupBy(x => x.CompetitionId)
                    .Select(g => g.First())
                    .Select(x => new
                    {
                        competitionId = x.CompetitionId,
                        tournamentId = x.TournamentId,
                        tournamentName = x.TournamentName,
                        competitionLabel = $"{x.DisciplineName} · {x.CategoryName?.Trim() ?? "—"} · {x.Gender}"
                    })
                    .OrderBy(x => x.tournamentName)
                    .ThenBy(x => x.competitionLabel)
                    .ToList();

                var players = playersRaw
                    .Where(p => p.TeamId == team.Id)
                    .Select(MapPlayer)
                    .ToList();

                return new
                {
                    team.Id,
                    team.Name,
                    team.Initials,
                    inscriptions,
                    players
                };
            }).ToList();

            return Ok(new { organizacionId = orgId, nombreEscuela = org?.Nombre, teams = teamsOut });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includeInactive = false)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            var showInactive = includeInactive;
            if (showInactive)
            {
                if (User?.Identity?.IsAuthenticated != true)
                    showInactive = false;
                else if (!TournDelegateAuth.IsTournamentAdmin(User))
                {
                    var myOrg = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
                    if (myOrg == null || team.OrganizacionId != myOrg.Value)
                        showInactive = false;
                }
            }

            IEnumerable<Player> playerQuery = team.Players;
            if (!showInactive)
                playerQuery = playerQuery.Where(p => p.IsActive);

            var playersOrdered = playerQuery
                .OrderBy(p => p.Number)
                .ThenBy(p => p.Name)
                .Select(p => new PlayerDto
                {
                    Id = p.Id,
                    TeamId = p.TeamId,
                    Name = p.Name,
                    Dni = p.Dni,
                    BirthDate = p.BirthDate,
                    Number = p.Number,
                    Position = p.Position,
                    PhotoUrl = p.PhotoUrl,
                    IsActive = p.IsActive,
                    IsEligible = p.IsEligible
                })
                .ToList();

            var response = new TeamDetailsDto
            {
                Id = team.Id,
                Name = team.Name,
                Initials = team.Initials,
                LogoUrl = team.LogoUrl,
                RepresentativeName = team.RepresentativeName,
                Players = playersOrdered
            };

            return Ok(response);
        }

        // --- CREACIÓN ---

        [HttpPost]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> Create([FromForm] CreateTeamDto dto)
        {
            // Delegado: el OrganizacionId viene siempre del usuario (no del formulario).
            int organizacionIdEquipo;
            if (!TournDelegateAuth.IsTournamentAdmin(User))
            {
                var myOrg = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
                if (myOrg == null) return BadRequest("Tu usuario no tiene escuela asignada.");
                organizacionIdEquipo = myOrg.Value;
            }
            else
            {
                organizacionIdEquipo = dto.OrganizacionId;
                if (organizacionIdEquipo <= 0)
                    return BadRequest("Indicá la organización (OrganizacionId).");
            }

            var org = await _context.Organizaciones.FindAsync(organizacionIdEquipo);
            if (org == null) return BadRequest("La organización no existe.");

            if (!OrganizationCanFieldTeams(org))
                return BadRequest(
                    "Solo se pueden crear equipos vinculados a una organización de tipo Escuela o Facultad.");

            string? logoUrl = dto.LogoFile != null
                ? await _storageService.UploadFileAsync(dto.LogoFile, "equipos")
                : null;

            var team = new Team
            {
                Name = dto.Name,
                OrganizacionId = organizacionIdEquipo,
                Initials = dto.Initials?.ToUpper(),
                RepresentativeName = dto.RepresentativeName,
                LogoUrl = logoUrl,
                IsActive = true
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
        }

        // --- EDICIÓN ---

        [HttpPut("{id}")]
        [Authorize(Policy = TournDelegateAuth.PolicyName)]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreateTeamDto dto)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

            if (!TournDelegateAuth.IsTournamentAdmin(User))
            {
                var myOrg = await TournDelegateAuth.GetOrganizacionIdAsync(User, _context);
                if (myOrg == null || team.OrganizacionId != myOrg.Value)
                    return Forbid();
            }

            if (dto.LogoFile != null)
            {
                team.LogoUrl = await _storageService.UploadFileAsync(dto.LogoFile, "equipos");
            }

            team.Name = dto.Name;
            team.Initials = dto.Initials?.ToUpper();
            team.RepresentativeName = dto.RepresentativeName;

            await _context.SaveChangesAsync();
            return Ok(team);
        }

        // --- ESTADO Y ELIMINACIÓN ---

        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

            team.IsActive = !team.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = team.IsActive });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var team = await _context.Teams
                .Include(t => t.GroupTeams)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            // Protección de integridad: Si el equipo ya está en un grupo (ya participó), no borrar.
            if (team.GroupTeams.Any())
                return BadRequest("No se puede eliminar: El equipo ya tiene historial en competiciones. Desactívelo.");

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Equipos de torneo se asocian a facultades o escuelas (no a la universidad raíz u otros tipos).</summary>
        private static bool OrganizationCanFieldTeams(Organizacion org)
        {
            var t = (org.Tipo ?? string.Empty).Trim();
            return t.Equals("Escuela", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Facultad", StringComparison.OrdinalIgnoreCase);
        }
    }
}
