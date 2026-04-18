using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Player;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlayersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public PlayersController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // --- CREACIÓN ---
        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create([FromForm] CreatePlayerDto dto)
        {
            // 1. Validar que el equipo exista
            if (!await _context.Teams.AnyAsync(t => t.Id == dto.TeamId))
                return BadRequest("El equipo especificado no existe.");

            if (await _context.Players.AnyAsync(p => p.Dni == dto.Dni))
                return BadRequest("El DNI ya se encuentra registrado.");

            string? photoUrl = dto.PhotoFile != null
                ? await _storageService.UploadFileAsync(dto.PhotoFile, "jugadores")
                : null;

            var player = new Player
            {
                TeamId = dto.TeamId,
                Name = dto.Name,
                Dni = dto.Dni,
                BirthDate = dto.BirthDate,
                Position = dto.Position ?? PlayerPosition.None,
                Number = dto.Number,
                PhotoUrl = photoUrl,
                IsActive = true,
                IsEligible = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return Ok(player);
        }

        // --- ACTUALIZACIÓN ---
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreatePlayerDto dto)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();

            if (dto.PhotoFile != null)
                player.PhotoUrl = await _storageService.UploadFileAsync(dto.PhotoFile, "jugadores");

            player.Name = dto.Name;
            player.Dni = dto.Dni;
            player.BirthDate = dto.BirthDate;
            player.Position = dto.Position ?? player.Position;
            player.Number = dto.Number;

            await _context.SaveChangesAsync();
            return Ok(player);
        }

        // --- BÚSQUEDA ---
        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetByTeam(Guid teamId)
        {
            return Ok(await _context.Players
                .Where(p => p.TeamId == teamId && p.IsActive)
                .OrderBy(p => p.Number) // Ordenar por dorsal es más natural en deportes
                .ToListAsync());
        }

        // --- ELIMINACIÓN ---
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound();

            player.IsActive = !player.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = player.IsActive });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var player = await _context.Players
                .Include(p => p.MatchEvents) // 🛡️ Protección
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null) return NotFound();

            if (player.MatchEvents.Any())
                return BadRequest("No se puede eliminar: El jugador tiene historial de goles/tarjetas. Desactívelo.");

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
