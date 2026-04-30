using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Authorization;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments;

[ApiController]
[Authorize]
public class MatchLineupsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MatchLineupsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("api/Matches/{matchId:guid}/lineups")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMatchLineups(Guid matchId)
    {
        var lineups = await _context.MatchLineups
            .AsNoTracking()
            .Where(l => l.MatchId == matchId)
            .Include(l => l.Team)
            .Include(l => l.Players)
                .ThenInclude(p => p.Player)
            .OrderBy(l => l.Team.Name)
            .Select(l => new
            {
                l.Id,
                l.MatchId,
                l.TeamId,
                TeamName = l.Team.Name,
                Status = l.Status.ToString(),
                l.SubmittedAt,
                l.LockedAt,
                l.Observation,
                StartersCount = l.Players.Count(p => p.Role == MatchLineupPlayerRole.Starter),
                SubstitutesCount = l.Players.Count(p => p.Role == MatchLineupPlayerRole.Substitute),
                Players = l.Players
                    .OrderBy(p => p.Role)
                    .ThenBy(p => p.ShirtNumber ?? p.Player.Number ?? 999)
                    .ThenBy(p => p.Player.Name)
                    .Select(p => new
                    {
                        p.Id,
                        p.PlayerId,
                        PlayerName = p.Player.Name,
                        Dni = p.Player.Dni,
                        Role = p.Role.ToString(),
                        Number = p.ShirtNumber ?? p.Player.Number,
                        Position = p.Position.ToString(),
                        p.IsCaptain,
                        p.IsGoalkeeper,
                        p.Observation
                    })
            })
            .ToListAsync();

        return Ok(lineups);
    }

    [HttpPut("api/Matches/{matchId:guid}/lineups/{teamId:guid}")]
    [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
    public async Task<IActionResult> UpsertLineup(Guid matchId, Guid teamId, [FromBody] UpsertMatchLineupDto dto)
    {
        var match = await _context.Matches
            .Include(m => m.Phase)
            .FirstOrDefaultAsync(m => m.Id == matchId);
        if (match == null) return NotFound("Partido no encontrado.");

        if (teamId != match.LocalTeamId && teamId != match.VisitorTeamId)
            return BadRequest("El equipo no pertenece a este partido.");

        var temporaryOpen = IsTemporaryLineupWindowOpen(match, teamId);
        if (match.Status == MatchStatus.Finalizado)
            return BadRequest("La planilla solo se puede modificar antes de iniciar el partido.");
        if (match.Status == MatchStatus.EnVivo && !temporaryOpen)
            return BadRequest("La planilla está cerrada por inicio de partido. Solicita apertura temporal a mesa.");

        if (!await TeamManagementAuthorization.CanSubmitMatchLineupAsync(User, _context, teamId))
            return Forbid();

        var competitionId = match.Phase.CompetitionId;
        await AutoLockRostersForMatchAsync(match);

        var rosterRow = await _context.CompetitionTeams
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.CompetitionId == competitionId && ct.TeamId == teamId);
        var canBypassRosterLock = TournDelegateAuth.IsTournamentAdmin(User)
            || User.Claims.Any(c => c.Value == Permissions.TournLineupManage);

        if (rosterRow?.RosterLocked == true && !canBypassRosterLock && !temporaryOpen)
            return BadRequest("La lista oficial está cerrada para este equipo en esta competencia.");

        var playerIds = dto.Players.Select(p => p.PlayerId).ToList();
        if (playerIds.Count == 0)
            return BadRequest("La planilla debe incluir al menos un jugador.");
        if (playerIds.Count != playerIds.Distinct().Count())
            return BadRequest("Un jugador no puede repetirse en la misma planilla.");

        var validPlayers = await _context.Players.AsNoTracking()
            .Where(p => playerIds.Contains(p.Id) && p.TeamId == teamId && p.IsActive && p.IsEligible)
            .Select(p => new { p.Id, p.Number, p.Position })
            .ToListAsync();
        if (validPlayers.Count != playerIds.Count)
            return BadRequest("La planilla contiene jugadores no activos, no habilitados o de otro equipo.");

        var sanctionedIds = await _context.PlayerSanctions.AsNoTracking()
            .Where(s => s.IsActive
                && playerIds.Contains(s.PlayerId)
                && (s.CompetitionId == null || s.CompetitionId == competitionId))
            .Select(s => s.PlayerId)
            .Distinct()
            .ToListAsync();
        if (sanctionedIds.Count > 0)
            return BadRequest("La planilla contiene jugadores con sanción activa.");

        if (!dto.Players.Any(p => p.Role == MatchLineupPlayerRole.Starter))
            return BadRequest("La planilla debe tener al menos un titular.");

        var lineup = await _context.MatchLineups
            .Include(l => l.Players)
            .FirstOrDefaultAsync(l => l.MatchId == matchId && l.TeamId == teamId);

        if (lineup == null)
        {
            lineup = new MatchLineup
            {
                MatchId = matchId,
                TeamId = teamId
            };
            _context.MatchLineups.Add(lineup);
            await _context.SaveChangesAsync();
        }

        // Reemplazo limpio de jugadores para evitar conflictos de tracking/índices únicos.
        await _context.MatchLineupPlayers
            .Where(p => p.MatchLineupId == lineup.Id)
            .ExecuteDeleteAsync();

        var now = DateTime.UtcNow;
        lineup.Status = dto.Lock ? MatchLineupStatus.Locked : MatchLineupStatus.Submitted;
        lineup.SubmittedAt = now;
        lineup.SubmittedByUsuarioId = GetUsuarioId();
        lineup.LockedAt = dto.Lock ? now : null;
        lineup.Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim();

        var validById = validPlayers.ToDictionary(p => p.Id);
        var resolvedNumbers = new List<int>();
        var toInsert = new List<MatchLineupPlayer>();
        foreach (var item in dto.Players)
        {
            var source = validById[item.PlayerId];
            var resolvedNumber = item.ShirtNumber ?? source.Number;
            if (!resolvedNumber.HasValue)
                return BadRequest("Todos los jugadores convocados deben tener número de camiseta.");
            resolvedNumbers.Add(resolvedNumber.Value);
            toInsert.Add(new MatchLineupPlayer
            {
                MatchLineupId = lineup.Id,
                PlayerId = item.PlayerId,
                Role = item.Role,
                ShirtNumber = resolvedNumber,
                Position = item.Position ?? source.Position,
                IsCaptain = item.IsCaptain,
                IsGoalkeeper = item.IsGoalkeeper,
                Observation = string.IsNullOrWhiteSpace(item.Observation) ? null : item.Observation.Trim()
            });
        }

        var repeated = resolvedNumbers
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => $"#{g.Key}")
            .ToList();
        if (repeated.Count > 0)
            return BadRequest($"No se puede guardar la planilla: números de camiseta repetidos ({string.Join(", ", repeated)}).");

        _context.MatchLineupPlayers.AddRange(toInsert);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("No se pudo guardar la planilla por un conflicto de datos. Actualizá la página e intentá nuevamente.");
        }
        return Ok(new { message = "Planilla guardada.", lineup.Id, status = lineup.Status.ToString() });
    }

    [HttpPost("api/Matches/{matchId:guid}/lineups/{teamId:guid}/temporary-open")]
    [Authorize(Policy = Permissions.TournMatchControl)]
    public async Task<IActionResult> OpenLineupTemporaryWindow(
        Guid matchId,
        Guid teamId,
        [FromBody] OpenLineupTemporaryWindowDto dto)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
        if (match == null) return NotFound("Partido no encontrado.");
        if (teamId != match.LocalTeamId && teamId != match.VisitorTeamId)
            return BadRequest("El equipo no pertenece a este partido.");
        if (match.Status == MatchStatus.Finalizado)
            return BadRequest("El partido ya finalizó.");

        var minutes = Math.Clamp(dto.Minutes, 1, 60);
        var until = DateTime.UtcNow.AddMinutes(minutes);
        if (teamId == match.LocalTeamId)
            match.LocalLineupOpenUntilUtc = until;
        else
            match.VisitorLineupOpenUntilUtc = until;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            matchId,
            teamId,
            openUntilUtc = until,
            minutes
        });
    }

    [HttpPost("api/Matches/{matchId:guid}/lineups/temporary-open-all")]
    [Authorize(Policy = Permissions.TournMatchControl)]
    public async Task<IActionResult> OpenLineupTemporaryWindowForBothTeams(
        Guid matchId,
        [FromBody] OpenLineupTemporaryWindowDto dto)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
        if (match == null) return NotFound("Partido no encontrado.");
        if (!match.LocalTeamId.HasValue || !match.VisitorTeamId.HasValue)
            return BadRequest("El partido debe tener ambos equipos asignados.");
        if (match.Status == MatchStatus.Finalizado)
            return BadRequest("El partido ya finalizó.");

        var minutes = Math.Clamp(dto.Minutes, 1, 60);
        var until = DateTime.UtcNow.AddMinutes(minutes);
        match.LocalLineupOpenUntilUtc = until;
        match.VisitorLineupOpenUntilUtc = until;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            matchId,
            localTeamId = match.LocalTeamId,
            visitorTeamId = match.VisitorTeamId,
            openUntilUtc = until,
            minutes
        });
    }

    [HttpPost("api/Matches/{matchId:guid}/lineups/temporary-close-all")]
    [Authorize(Policy = Permissions.TournMatchControl)]
    public async Task<IActionResult> CloseLineupTemporaryWindowForBothTeams(Guid matchId)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
        if (match == null) return NotFound("Partido no encontrado.");

        match.LocalLineupOpenUntilUtc = null;
        match.VisitorLineupOpenUntilUtc = null;

        await _context.SaveChangesAsync();
        return Ok(new
        {
            matchId,
            message = "Ventana temporal de planilla cerrada para ambos equipos."
        });
    }

    [HttpPatch("api/Competitions/{competitionId:guid}/teams/{teamId:guid}/roster-lock")]
    [Authorize(Policy = Permissions.TournLineupManage)]
    public async Task<IActionResult> SetRosterLock(Guid competitionId, Guid teamId, [FromBody] SetRosterLockDto dto)
    {
        var row = await _context.CompetitionTeams
            .FirstOrDefaultAsync(ct => ct.CompetitionId == competitionId && ct.TeamId == teamId);
        if (row == null)
            return NotFound("El equipo no está inscrito en esta competencia.");

        var now = DateTime.UtcNow;
        row.RosterLocked = dto.Locked;
        if (dto.Locked)
        {
            row.RosterLockedAt = now;
            row.RosterLockedByUsuarioId = GetUsuarioId();
        }
        else
        {
            row.RosterUnlockedAt = now;
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            row.CompetitionId,
            row.TeamId,
            row.RosterLocked,
            row.RosterLockedAt,
            row.RosterUnlockedAt
        });
    }

    [HttpPatch("api/Competitions/{competitionId:guid}/rosters/lock-all")]
    [Authorize(Policy = Permissions.TournLineupManage)]
    public Task<IActionResult> LockCompetitionRosters(Guid competitionId) =>
        SetCompetitionRosterLockAsync(competitionId, true);

    [HttpPatch("api/Competitions/{competitionId:guid}/rosters/unlock-all")]
    [Authorize(Policy = Permissions.TournLineupManage)]
    public Task<IActionResult> UnlockCompetitionRosters(Guid competitionId) =>
        SetCompetitionRosterLockAsync(competitionId, false);

    [HttpPatch("api/Tournaments/{tournamentId:guid}/rosters/lock-all")]
    [Authorize(Policy = Permissions.TournLineupManage)]
    public Task<IActionResult> LockTournamentRosters(Guid tournamentId) =>
        SetTournamentRosterLockAsync(tournamentId, true);

    [HttpPatch("api/Tournaments/{tournamentId:guid}/rosters/unlock-all")]
    [Authorize(Policy = Permissions.TournLineupManage)]
    public Task<IActionResult> UnlockTournamentRosters(Guid tournamentId) =>
        SetTournamentRosterLockAsync(tournamentId, false);

    private int? GetUsuarioId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }

    private async Task<IActionResult> SetCompetitionRosterLockAsync(Guid competitionId, bool locked)
    {
        var rows = await _context.CompetitionTeams
            .Where(ct => ct.CompetitionId == competitionId)
            .ToListAsync();
        if (rows.Count == 0)
            return NotFound("La competencia no tiene equipos inscritos.");

        ApplyRosterLock(rows, locked);
        await _context.SaveChangesAsync();

        return Ok(new { competitionId, locked, affected = rows.Count });
    }

    private async Task<IActionResult> SetTournamentRosterLockAsync(Guid tournamentId, bool locked)
    {
        var rows = await _context.CompetitionTeams
            .Where(ct => ct.Competition.TournamentId == tournamentId)
            .ToListAsync();
        if (rows.Count == 0)
            return NotFound("El torneo no tiene equipos inscritos en competencias.");

        ApplyRosterLock(rows, locked);
        await _context.SaveChangesAsync();

        return Ok(new { tournamentId, locked, affected = rows.Count });
    }

    private void ApplyRosterLock(IEnumerable<CompetitionTeam> rows, bool locked)
    {
        var now = DateTime.UtcNow;
        var uid = GetUsuarioId();
        foreach (var row in rows)
        {
            row.RosterLocked = locked;
            if (locked)
            {
                row.RosterLockedAt = now;
                row.RosterLockedByUsuarioId = uid;
            }
            else
            {
                row.RosterUnlockedAt = now;
            }
        }
    }

    private async Task AutoLockRostersForMatchAsync(Match match)
    {
        if (match.ScheduledAt.Year < 1900)
            return;

        var scheduled = DateTime.SpecifyKind(match.ScheduledAt, DateTimeKind.Local).ToUniversalTime();
        var lockMinutesBeforeMatch = await ResolveLineupCloseMinutesAsync(match.DisciplineId);
        if (scheduled > DateTime.UtcNow.AddMinutes(lockMinutesBeforeMatch))
            return;

        var teamIds = new[] { match.LocalTeamId, match.VisitorTeamId }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();
        if (teamIds.Count == 0)
            return;

        var rows = await _context.CompetitionTeams
            .Where(ct => ct.CompetitionId == match.Phase.CompetitionId
                && teamIds.Contains(ct.TeamId)
                && !ct.RosterLocked)
            .ToListAsync();

        ApplyRosterLock(rows, true);
    }

    private async Task<int> ResolveLineupCloseMinutesAsync(Guid disciplineId)
    {
        const int fallbackMinutes = 5;
        var value = await _context.DisciplineRules
            .AsNoTracking()
            .Where(r => r.DisciplineId == disciplineId && r.RuleKey == "CIERRE_PLANILLA_MINUTOS_ANTES")
            .Select(r => r.RuleValue)
            .FirstOrDefaultAsync();
        if (int.TryParse(value, out var parsed))
            return Math.Clamp(parsed, 0, 120);
        return fallbackMinutes;
    }

    private static bool IsTemporaryLineupWindowOpen(Match match, Guid teamId)
    {
        var now = DateTime.UtcNow;
        if (teamId == match.LocalTeamId)
            return match.LocalLineupOpenUntilUtc.HasValue && match.LocalLineupOpenUntilUtc.Value > now;
        if (teamId == match.VisitorTeamId)
            return match.VisitorLineupOpenUntilUtc.HasValue && match.VisitorLineupOpenUntilUtc.Value > now;
        return false;
    }
}

public sealed class UpsertMatchLineupDto
{
    public bool Lock { get; set; } = true;
    public string? Observation { get; set; }
    public List<UpsertMatchLineupPlayerDto> Players { get; set; } = new();
}

public sealed class UpsertMatchLineupPlayerDto
{
    public Guid PlayerId { get; set; }
    public MatchLineupPlayerRole Role { get; set; }
    public int? ShirtNumber { get; set; }
    public PlayerPosition? Position { get; set; }
    public bool IsCaptain { get; set; }
    public bool IsGoalkeeper { get; set; }
    public string? Observation { get; set; }
}

public sealed class SetRosterLockDto
{
    public bool Locked { get; set; }
}

public sealed class OpenLineupTemporaryWindowDto
{
    public int Minutes { get; set; } = 5;
}
