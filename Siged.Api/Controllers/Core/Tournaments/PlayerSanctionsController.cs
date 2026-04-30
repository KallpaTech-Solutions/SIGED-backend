using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PlayerSanctionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PlayerSanctionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("player/{playerId:guid}")]
    [Authorize(Policy = Permissions.TournPlayerSanctionManage)]
    public async Task<IActionResult> GetByPlayer(Guid playerId)
    {
        var rows = await _context.PlayerSanctions
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.PlayerId,
                PlayerName = s.Player.Name,
                s.CompetitionId,
                CompetitionName = s.Competition != null ? s.Competition.Tournament.Name : null,
                s.TeamId,
                TeamName = s.Team != null ? s.Team.Name : null,
                s.MatchId,
                Type = s.Type.ToString(),
                s.Reason,
                s.MatchesCount,
                s.PhasesCount,
                s.StartsAt,
                s.EndsAt,
                s.IsActive,
                s.CreatedAt,
                s.LiftedAt,
                s.Observation
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.TournPlayerSanctionManage)]
    public async Task<IActionResult> Create([FromBody] CreatePlayerSanctionDto dto)
    {
        var player = await _context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.PlayerId);
        if (player == null) return NotFound("Jugador no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest("Indicá el motivo de la sanción.");

        if (dto.Type == PlayerSanctionType.Matches && (!dto.MatchesCount.HasValue || dto.MatchesCount <= 0))
            return BadRequest("Indicá cuántas fechas aplica la sanción.");

        if (dto.Type == PlayerSanctionType.Phases && (!dto.PhasesCount.HasValue || dto.PhasesCount <= 0))
            return BadRequest("Indicá cuántas fases aplica la sanción.");

        if (dto.Type == PlayerSanctionType.UntilDate && dto.EndsAt == null)
            return BadRequest("Indicá la fecha de fin de sanción.");

        var sanction = new PlayerSanction
        {
            PlayerId = dto.PlayerId,
            CompetitionId = dto.CompetitionId,
            TeamId = dto.TeamId ?? player.TeamId,
            MatchId = dto.MatchId,
            MatchEventId = dto.MatchEventId,
            Type = dto.Type,
            Reason = dto.Reason.Trim(),
            MatchesCount = dto.MatchesCount,
            PhasesCount = dto.PhasesCount,
            StartsAt = dto.StartsAt ?? DateTime.UtcNow,
            EndsAt = dto.EndsAt,
            IsActive = true,
            CreatedByUsuarioId = GetUsuarioId(),
            CreatedAt = DateTime.UtcNow,
            Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim()
        };

        _context.PlayerSanctions.Add(sanction);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Sanción registrada.", sanction.Id });
    }

    [HttpPatch("{id:guid}/lift")]
    [Authorize(Policy = Permissions.TournPlayerSanctionManage)]
    public async Task<IActionResult> Lift(Guid id)
    {
        var sanction = await _context.PlayerSanctions.FirstOrDefaultAsync(s => s.Id == id);
        if (sanction == null) return NotFound("Sanción no encontrada.");

        sanction.IsActive = false;
        sanction.LiftedAt = DateTime.UtcNow;
        sanction.LiftedByUsuarioId = GetUsuarioId();

        await _context.SaveChangesAsync();
        return Ok(new { message = "Sanción levantada.", sanction.Id });
    }

    private int? GetUsuarioId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }
}

public sealed class CreatePlayerSanctionDto
{
    public Guid PlayerId { get; set; }
    public Guid? CompetitionId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? MatchId { get; set; }
    public Guid? MatchEventId { get; set; }
    public PlayerSanctionType Type { get; set; } = PlayerSanctionType.Matches;
    public string Reason { get; set; } = string.Empty;
    public int? MatchesCount { get; set; }
    public int? PhasesCount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Observation { get; set; }
}
