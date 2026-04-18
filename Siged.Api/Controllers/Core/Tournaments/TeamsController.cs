using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Player;
using Siged.Application.DTOs.Tournaments.Team;
using Siged.Application.Interfaces.Almacenamiento;
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            // Mapeamos a nuestro DTO de detalle
            var response = new TeamDetailsDto
            {
                Id = team.Id,
                Name = team.Name,
                Initials = team.Initials,
                LogoUrl = team.LogoUrl,
                RepresentativeName = team.RepresentativeName,
                Players = team.Players
                    .Where(p => p.IsActive)
                    .Select(p => new PlayerDto
                    {
                        Id = p.Id,
                        TeamId = p.TeamId,
                        Name = p.Name,
                        Dni = p.Dni,
                        Number = p.Number,
                        Position = p.Position,
                        PhotoUrl = p.PhotoUrl,
                        IsActive = p.IsActive,
                        IsEligible = p.IsEligible
                    }).ToList()
            };

            return Ok(response);
        }

        // --- CREACIÓN ---

        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create([FromForm] CreateTeamDto dto)
        {
            // 🛡️ Validación de Ingeniería: ¿La escuela existe y es válida?
            var org = await _context.Organizaciones.FindAsync(dto.OrganizacionId);
            if (org == null) return BadRequest("La organización no existe.");

            if (org.Tipo != "Escuela")
                return BadRequest("Solo se pueden crear equipos vinculados a una 'Escuela'.");

            string? logoUrl = dto.LogoFile != null
                ? await _storageService.UploadFileAsync(dto.LogoFile, "equipos")
                : null;

            var team = new Team
            {
                Name = dto.Name,
                OrganizacionId = dto.OrganizacionId, // 🔗 El vínculo vital
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
        public async Task<IActionResult> Update(Guid id, [FromForm] CreateTeamDto dto)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

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
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

            team.IsActive = !team.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = team.IsActive });
        }

        [HttpDelete("{id}")]
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
    }
}
