using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Security; 
using Siged.Infrastructure.Persistence;
using System.Linq;
using System.Net.Mime;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    public class TournamentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public TournamentsController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        /// <summary>
        /// Obtiene el listado de torneos registrados.
        /// </summary>
        /// <param name="includeInactive">Si es true, incluye torneos marcados como inactivos.</param>
        [HttpGet]
        [AllowAnonymous] // Permitir acceso público para listar torneos
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _context.Tournaments.AsQueryable();
            if (!includeInactive) query = query.Where(t => t.IsActive);

            return Ok(await query.OrderByDescending(t => t.Year).ToListAsync());
        }

        /// <summary>
        /// Obtiene la lista de años en los que se han registrado torneos (para filtros en el Front).
        /// </summary>
        [HttpGet("years")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableYears()
        {
            var years = await _context.Tournaments
                .Select(t => t.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            return Ok(years);
        }
        /// <summary>
        /// Obtiene el detalle de un torneo, incluyendo sus disciplinas y competencias.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Permitir acceso público para ver detalles de torneos
        public async Task<IActionResult> GetById(Guid id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Competitions)
                    .ThenInclude(c => c.Discipline)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();
            return Ok(tournament);
        }
        /// <summary>
        /// Obtiene métricas rápidas del torneo (Equipos, Partidos, Jugadores).
        /// </summary>
        [HttpGet("{id}/stats")]
        [Authorize(Policy = Permissions.TournView)]
        public async Task<IActionResult> GetTournamentStats(Guid id)
        {
            var tournamentExists = await _context.Tournaments.AnyAsync(t => t.Id == id);
            if (!tournamentExists) return NotFound("Torneo no encontrado.");

            // 1. Obtenemos los IDs de las competencias
            var competitionIds = await _context.Competitions
                .Where(c => c.TournamentId == id)
                .Select(c => c.Id)
                .ToListAsync();

            // 2. Obtenemos los IDs de las fases
            var phaseIds = await _context.Phases
                .Where(p => competitionIds.Contains(p.CompetitionId))
                .Select(p => p.Id)
                .ToListAsync();

            // 3. Obtenemos los IDs de los grupos
            var groupIds = await _context.Groups
                .Where(g => phaseIds.Contains(g.PhaseId))
                .Select(g => g.Id)
                .ToListAsync();

            var stats = new
            {
                // 🛡️ CORRECCIÓN TEAM: Si Team no tiene CompetitionId, 
                // probablemente la relación es inversa desde Competition.
                TotalEquipos = await _context.Teams
                    .CountAsync(t => _context.GroupTeams.Any(gt => groupIds.Contains(gt.GroupId) && gt.TeamId == t.Id)),

                // 🛡️ CORRECCIÓN MATCH: Usamos m.GroupId (el campo ID) directamente 
                // y manejamos el Nullable con .HasValue
                PartidosJugados = await _context.Matches
                    .CountAsync(m => m.GroupId.HasValue && groupIds.Contains(m.GroupId.Value)
                                && m.Status == Domain.Entities.Core.Tournaments.Enums.MatchStatus.Finalizado),

                PartidosPendientes = await _context.Matches
                    .CountAsync(m => m.GroupId.HasValue && groupIds.Contains(m.GroupId.Value)
                                && m.Status != Domain.Entities.Core.Tournaments.Enums.MatchStatus.Finalizado),

                TotalGoles = await _context.MatchEvents
                    .CountAsync(e => e.Match != null && e.Match.GroupId.HasValue && groupIds.Contains(e.Match.GroupId.Value)
                                && e.Type == Domain.Entities.Core.Tournaments.Enums.MatchEventType.Goal)
            };

            return Ok(stats);
        }
        // --- CREACIÓN Y EDICIÓN ---
        /// <summary>
        /// Crea un nuevo torneo institucional.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create([FromForm] CreateTournamentDto dto)
        {
            string? logoUrl = dto.LogoFile != null
                ? await _storageService.UploadFileAsync(dto.LogoFile, "torneos")
                : null;

            var tournament = new Tournament
            {
                Name = dto.Name,
                Year = dto.Year,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Organizer = dto.Organizer,
                LogoUrl = logoUrl,
                IsActive = true
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = tournament.Id }, tournament);
        }

        /// <summary>
        /// Actualiza la información básica de un torneo.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreateTournamentDto dto)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            // Si sube un nuevo logo, podrías implementar lógica para borrar el anterior en Supabase aquí
            if (dto.LogoFile != null)
            {
                tournament.LogoUrl = await _storageService.UploadFileAsync(dto.LogoFile, "torneos");
            }

            tournament.Name = dto.Name;
            tournament.Year = dto.Year;
            tournament.Description = dto.Description;
            tournament.StartDate = dto.StartDate;
            tournament.EndDate = dto.EndDate;
            tournament.Organizer = dto.Organizer;

            await _context.SaveChangesAsync();
            return Ok(tournament);
        }

        // --- ESTADO Y ELIMINACIÓN ---
        /// <summary>
        /// Cambia el estado (Activo/Inactivo) de un torneo.
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            tournament.IsActive = !tournament.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = tournament.IsActive });
        }


        /// <summary>  
        /// Elimina un torneo de forma permanente. Solo permitido si no tiene disciplinas configuradas (Nivel Ingeniería).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.SecurityRoleManage)] // Solo SuperAdmin puede eliminar definitivamente por la criticidad de esta acción
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Competitions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();

            // Validación de Ingeniería: No borrar si ya tiene disciplinas configuradas
            if (tournament.Competitions.Any())
                return BadRequest("No se puede eliminar: El torneo ya tiene competiciones asignadas. Desactívelo en su lugar.");

            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}