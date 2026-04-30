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

            await AutoLockDueRostersForTeamAsync(dto.TeamId);

            if (await _context.CompetitionTeams.AsNoTracking().AnyAsync(ct => ct.TeamId == dto.TeamId && ct.RosterLocked))
                return BadRequest("La lista oficial del equipo está cerrada en una competencia. Reabrila antes de registrar nuevos jugadores.");

            var dni = (dto.Dni ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(dni))
                return BadRequest("Indicá el código de identificación del jugador.");

            if (await _context.Players.AnyAsync(p => p.TeamId == dto.TeamId && p.Dni == dni))
                return BadRequest("Este código ya está registrado en este equipo.");

            if (await DniUsedInSharedCompetitionAsync(dni, dto.TeamId))
                return BadRequest(
                    "Este jugador ya figura en otro equipo inscrito en la misma competencia. Podés repetir el código solo en equipos de competencias distintas.");

            string? photoUrl = dto.PhotoFile != null
                ? await _storageService.UploadFileAsync(dto.PhotoFile, "jugadores")
                : null;

            var player = new Player
            {
                TeamId = dto.TeamId,
                Name = dto.Name,
                Dni = dni,
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

            var dni = (dto.Dni ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(dni))
                return BadRequest("Indicá el código de identificación del jugador.");

            if (await _context.Players.AnyAsync(p =>
                    p.TeamId == player.TeamId && p.Dni == dni && p.Id != id))
                return BadRequest("Este código ya está registrado en este equipo.");

            if (await DniUsedInSharedCompetitionAsync(dni, player.TeamId, id))
                return BadRequest(
                    "Este jugador ya figura en otro equipo inscrito en la misma competencia. Podés repetir el código solo en equipos de competencias distintas.");

            if (dto.PhotoFile != null)
                player.PhotoUrl = await _storageService.UploadFileAsync(dto.PhotoFile, "jugadores");

            player.Name = dto.Name;
            player.Dni = dni;
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

        /// <summary>Cambia si el jugador está habilitado para jugar. Lo gestionan administración, encargados o mesa de control.</summary>
        [HttpPatch("{id}/eligibility")]
        [Authorize]
        public async Task<IActionResult> SetEligibility(Guid id, [FromBody] PlayerEligibilityDto dto)
        {
            if (!CanValidatePlayerEligibility())
                return Forbid();

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            player.IsEligible = dto.IsEligible;
            await _context.SaveChangesAsync();
            return Ok(new { id, isEligible = player.IsEligible });
        }

        /// <summary>Borrado físico para jugadores sin historial; delegados/co-delegados solo en equipos que gestionan.</summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var player = await _context.Players
                .Include(p => p.Team)
                .Include(p => p.MatchEvents) // 🛡️ Protección
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null) return NotFound();

            if (!TournDelegateAuth.IsTournamentAdmin(User)
                && !await TeamManagementAuthorization.CanManageTeamAsync(User, _context, player.TeamId))
                return Forbid();

            if (player.MatchEvents.Any())
                return BadRequest("No se puede eliminar: El jugador tiene historial de goles/tarjetas. Desactívelo.");

            if (await _context.MatchLineupPlayers.AsNoTracking().AnyAsync(x => x.PlayerId == id))
                return BadRequest("No se puede eliminar: El jugador ya figura en una planilla de partido. Desactívelo.");

            if (await _context.PlayerSanctions.AsNoTracking().AnyAsync(x => x.PlayerId == id))
                return BadRequest("No se puede eliminar: El jugador tiene historial de sanciones. Desactívelo.");

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool CanValidatePlayerEligibility()
        {
            return User.HasClaim("permission", Permissions.TournManage)
                || User.HasClaim("permission", Permissions.TournPlayerSanctionManage)
                || User.HasClaim("permission", Permissions.TournMatchControl);
        }

        /// <summary>
        /// True si ya existe otro jugador con el mismo DNI en un equipo que comparte al menos una competencia con <paramref name="teamId"/>.
        /// </summary>
        private async Task<bool> DniUsedInSharedCompetitionAsync(string dni, Guid teamId, Guid? excludePlayerId = null)
        {
            var competitionIds = await _context.CompetitionTeams.AsNoTracking()
                .Where(ct => ct.TeamId == teamId)
                .Select(ct => ct.CompetitionId)
                .ToListAsync();
            if (competitionIds.Count == 0)
                return false;

            var otherTeamsQuery = _context.Players.AsNoTracking()
                .Where(p => p.Dni == dni && p.TeamId != teamId);
            if (excludePlayerId.HasValue)
                otherTeamsQuery = otherTeamsQuery.Where(p => p.Id != excludePlayerId.Value);

            var otherTeamIds = await otherTeamsQuery
                .Select(p => p.TeamId)
                .Distinct()
                .ToListAsync();
            if (otherTeamIds.Count == 0)
                return false;

            return await _context.CompetitionTeams.AsNoTracking()
                .AnyAsync(ct => competitionIds.Contains(ct.CompetitionId) && otherTeamIds.Contains(ct.TeamId));
        }

        private async Task AutoLockDueRostersForTeamAsync(Guid teamId)
        {
            var dueCompetitionIds = await _context.Matches
                .AsNoTracking()
                .Where(m => m.Status == MatchStatus.Programado
                    && m.ScheduledAt.Year >= 1900
                    && (m.LocalTeamId == teamId || m.VisitorTeamId == teamId))
                .Select(m => new { m.ScheduledAt, m.Phase.CompetitionId })
                .ToListAsync();

            var nowLimit = DateTime.UtcNow.AddMinutes(5);
            var ids = dueCompetitionIds
                .Where(x => DateTime.SpecifyKind(x.ScheduledAt, DateTimeKind.Local).ToUniversalTime() <= nowLimit)
                .Select(x => x.CompetitionId)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return;

            var rows = await _context.CompetitionTeams
                .Where(ct => ct.TeamId == teamId && ids.Contains(ct.CompetitionId) && !ct.RosterLocked)
                .ToListAsync();
            if (rows.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var uid = TeamManagementAuthorization.GetUsuarioIdFromClaims(User);
            foreach (var row in rows)
            {
                row.RosterLocked = true;
                row.RosterLockedAt = now;
                row.RosterLockedByUsuarioId = uid;
            }

            await _context.SaveChangesAsync();
        }
    }

    public class PlayerEligibilityDto
    {
        public bool IsEligible { get; set; }
    }
}
