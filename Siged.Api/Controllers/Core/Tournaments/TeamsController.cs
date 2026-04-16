using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Player;
using Siged.Application.DTOs.Tournaments.Team;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public TeamsController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // --- LECTURA ---

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        {
            var query = _context.Teams.AsQueryable();
            if (onlyActive) query = query.Where(t => t.IsActive);

            return Ok(await query.OrderBy(t => t.Name).ToListAsync());
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
        public async Task<IActionResult> Create([FromForm] CreateTeamDto dto)
        {
            // 1. Subir logo/escudo al bucket "equipos"
            string? logoUrl = null;
            if (dto.LogoFile != null)
            {
                logoUrl = await _storageService.UploadFileAsync(dto.LogoFile, "equipos");
            }

            // 2. Crear entidad
            var team = new Team
            {
                Name = dto.Name,
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
