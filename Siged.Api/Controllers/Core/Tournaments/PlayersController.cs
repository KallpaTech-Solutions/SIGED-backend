using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Authorization;
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
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> Create([FromForm] CreatePlayerDto dto)
        {
            var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == dto.TeamId);
            if (team == null)
                return BadRequest("El equipo especificado no existe.");

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, dto.TeamId))
                return Forbid();

            if (await _context.Players.AnyAsync(p => p.Dni == dto.Dni))
                return BadRequest("Ese código de identificación ya está registrado.");

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
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreatePlayerDto dto)
        {
            var player = await _context.Players
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, player.TeamId))
                return Forbid();

            if (await _context.Players.AnyAsync(p => p.Dni == dto.Dni && p.Id != id))
                return BadRequest("Ese código de identificación ya está registrado.");

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

        // --- ELIMINACIÓN / ESTADO ---
        /// <summary>Activa o desactiva un jugador. Delegados solo sobre planteles de su escuela; administración de torneo sin esa restricción.</summary>
        [HttpPatch("{id}/status")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var player = await _context.Players
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, player.TeamId))
                return Forbid();

            player.IsActive = !player.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { id, isActive = player.IsActive });
        }

        /// <summary>Borrado físico solo para quienes administran torneos (no delegados de escuela).</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var player = await _context.Players
                .Include(p => p.Team)
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
