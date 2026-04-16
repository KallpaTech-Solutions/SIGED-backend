using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class TournamentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public TournamentsController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // --- LECTURA ---

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _context.Tournaments.AsQueryable();
            if (!includeInactive) query = query.Where(t => t.IsActive);

            return Ok(await query.OrderByDescending(t => t.Year).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Competitions)
                    .ThenInclude(c => c.Discipline)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound();
            return Ok(tournament);
        }

        // --- CREACIÓN Y EDICIÓN ---

        [HttpPost]
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

        [HttpPut("{id}")]
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

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            tournament.IsActive = !tournament.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = tournament.IsActive });
        }

        [HttpDelete("{id}")]
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