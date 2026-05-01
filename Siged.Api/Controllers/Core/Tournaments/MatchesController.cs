using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Authorization;
using Siged.Api.Hubs;
using Siged.Api.Services;
using Siged.Application.DTOs.Tournaments.Match;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;
using Siged.Api.Reports;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private const string DefaultActaLogoLeftKey = "ACTA_DEFAULT_LOGO_LEFT_URL";
        private const string DefaultActaLogoRightKey = "ACTA_DEFAULT_LOGO_RIGHT_URL";
        private const string DisciplineActaLogoLeftRuleKey = "ACTA_LOGO_LEFT_URL";
        private const string DisciplineActaLogoRightRuleKey = "ACTA_LOGO_RIGHT_URL";

        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TournamentHub> _hubContext;
        private readonly StandingsService _standingsService;
        private readonly TournamentVitrinaBroadcastService _vitrina;
        private readonly MatchSportRulesBuilder _sportRulesBuilder;
        private readonly MatchBroadcastWidgetStore _broadcastWidgetStore;

        public MatchesController(
            ApplicationDbContext context,
            IHubContext<TournamentHub> hubContext,
            StandingsService standingsService,
            TournamentVitrinaBroadcastService vitrina,
            MatchSportRulesBuilder sportRulesBuilder,
            MatchBroadcastWidgetStore broadcastWidgetStore)
        {
            _context = context;
            _hubContext = hubContext;
            _standingsService = standingsService;
            _vitrina = vitrina;
            _sportRulesBuilder = sportRulesBuilder;
            _broadcastWidgetStore = broadcastWidgetStore;
        }

        private async Task<bool> PlayersBelongToTeamAsync(Guid teamId, Guid? playerId, Guid? relatedPlayerId)
        {
            foreach (var pid in new[] { playerId, relatedPlayerId })
            {
                if (!pid.HasValue) continue;
                var ok = await _context.Players.AsNoTracking()
                    .AnyAsync(p => p.Id == pid.Value && p.TeamId == teamId);
                if (!ok) return false;
            }

            return true;
        }

        private async Task<string?> ValidatePlayersForMatchActaAsync(Match match, Guid teamId, Guid? playerId, Guid? relatedPlayerId)
        {
            var playerIds = new[] { playerId, relatedPlayerId }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();
            if (playerIds.Count == 0)
                return null;

            var lineup = await _context.MatchLineups.AsNoTracking()
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.MatchId == match.Id && l.TeamId == teamId);

            if (lineup != null && lineup.Status != MatchLineupStatus.Draft)
            {
                var allowed = lineup.Players.Select(p => p.PlayerId).ToHashSet();
                if (playerIds.Any(pid => !allowed.Contains(pid)))
                    return "El jugador no está incluido en la planilla enviada para este partido.";
            }

            var competitionId = await _context.Matches.AsNoTracking()
                .Where(m => m.Id == match.Id)
                .Select(m => m.Phase.CompetitionId)
                .FirstOrDefaultAsync();

            var hasSanction = await _context.PlayerSanctions.AsNoTracking()
                .AnyAsync(s => s.IsActive
                    && playerIds.Contains(s.PlayerId)
                    && (s.CompetitionId == null || s.CompetitionId == competitionId));

            return hasSanction ? "El jugador tiene una sanción activa y no puede registrarse en el acta." : null;
        }

        private async Task PushBroadcastAfterActaAsync(Guid matchId, Match match, object? lastEvent = null)
        {
            var widgetCur = _broadcastWidgetStore.GetSnapshotJson(matchId);
            var widgetMerged = await MatchBroadcastWidgetActaSync.MergeAfterActaChangeAsync(_context, matchId, match, widgetCur);
            using (var wDoc = JsonDocument.Parse(widgetMerged))
            {
                _broadcastWidgetStore.TrySetSnapshot(matchId, wDoc.RootElement, out _);
            }

            var broadcastSnapshot = _broadcastWidgetStore.GetSnapshotJson(matchId);
            await _hubContext.Clients.Group(MatchRoomGroup(matchId)).SendAsync("ReceiveMatchUpdate", new
            {
                matchId,
                status = match.Status.ToString(),
                localScore = match.LocalScore,
                visitorScore = match.VisitorScore,
                localPenaltyScore = match.LocalPenaltyScore ?? 0,
                visitorPenaltyScore = match.VisitorPenaltyScore ?? 0,
                clockAccumulatedSeconds = match.ClockAccumulatedSeconds,
                clockPeriodAnchorUtc = match.ClockPeriodAnchorUtc,
                clockWidgetKind = match.ClockWidgetKind.ToString(),
                broadcastWidgetJson = broadcastSnapshot,
                lastEvent = lastEvent ?? new { type = "ACTA_UPDATED", message = "Eventos del acta actualizados." }
            });
        }

        /// <summary>
        /// Vitrina pública (/torneos): partidos en vivo o del día (UTC), sin conocer journalId.
        /// Prioriza <see cref="MatchStatus.EnVivo"/>; incluye el resto del día en la misma lista (el front puede separar).
        /// </summary>
        [HttpGet("public/landing")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicLandingMatches([FromQuery] string? date = null)
        {
            DateTime dayStart;
            DateTime dayEnd;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
            {
                dayStart = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
                dayEnd = dayStart.AddDays(1);
            }
            else
            {
                dayStart = DateTime.UtcNow.Date;
                dayEnd = dayStart.AddDays(1);
            }

            var matches = await _context.Matches
                .AsNoTracking()
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .Include(m => m.Venue)
                .Include(m => m.Discipline)
                .Include(m => m.Phase)
                .Where(m => m.IsActive && (
                    m.Status == MatchStatus.EnVivo
                    || (m.ScheduledAt >= dayStart && m.ScheduledAt < dayEnd)))
                .OrderByDescending(m => m.Status == MatchStatus.EnVivo)
                .ThenBy(m => m.ScheduledAt)
                .Take(48)
                .Select(m => new
                {
                    m.Id,
                    CompetitionId = m.Phase.CompetitionId,
                    Status = m.Status,
                    m.ScheduledAt,
                    m.LocalScore,
                    m.VisitorScore,
                    DisciplineName = m.Discipline.Name,
                    VenueName = m.Venue != null ? m.Venue.Name : null,
                    LocalTeam = m.LocalTeam != null
                        ? new { m.LocalTeam.Name, m.LocalTeam.LogoUrl }
                        : null,
                    VisitorTeam = m.VisitorTeam != null
                        ? new { m.VisitorTeam.Name, m.VisitorTeam.LogoUrl }
                        : null
                })
                .ToListAsync();

            return Ok(matches);
        }

        /// <summary>
        /// Vitrina pública por competencia: mismos criterios que <see cref="GetPublicLandingMatches"/> pero solo
        /// partidos cuya fase pertenece a la competencia indicada (página pública de competencia).
        /// </summary>
        [HttpGet("public/by-competition/{competitionId:guid}/landing")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicLandingMatchesByCompetition(
            Guid competitionId,
            [FromQuery] string? date = null)
        {
            DateTime dayStart;
            DateTime dayEnd;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
            {
                dayStart = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
                dayEnd = dayStart.AddDays(1);
            }
            else
            {
                dayStart = DateTime.UtcNow.Date;
                dayEnd = dayStart.AddDays(1);
            }

            var matches = await _context.Matches
                .AsNoTracking()
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .Include(m => m.Venue)
                .Include(m => m.Discipline)
                .Include(m => m.Phase)
                .Where(m =>
                    m.IsActive
                    && m.Phase.CompetitionId == competitionId
                    && (
                        m.Status == MatchStatus.EnVivo
                        || (m.ScheduledAt >= dayStart && m.ScheduledAt < dayEnd)))
                .OrderByDescending(m => m.Status == MatchStatus.EnVivo)
                .ThenBy(m => m.ScheduledAt)
                .Take(48)
                .Select(m => new
                {
                    m.Id,
                    CompetitionId = m.Phase.CompetitionId,
                    Status = m.Status,
                    m.ScheduledAt,
                    m.LocalScore,
                    m.VisitorScore,
                    DisciplineName = m.Discipline.Name,
                    VenueName = m.Venue != null ? m.Venue.Name : null,
                    LocalTeam = m.LocalTeam != null
                        ? new { m.LocalTeam.Name, m.LocalTeam.LogoUrl }
                        : null,
                    VisitorTeam = m.VisitorTeam != null
                        ? new { m.VisitorTeam.Name, m.VisitorTeam.LogoUrl }
                        : null
                })
                .ToListAsync();

            return Ok(matches);
        }

        /// <summary>
        /// Vitrina pública: marcador, contexto (torneo/competencia) y cronología de eventos.
        /// Solo partidos activos (<see cref="Match.IsActive"/>).
        /// </summary>
        [HttpGet("public/{id:guid}/detail")]
        [AllowAnonymous]
        public Task<IActionResult> GetPublicMatchDetail(Guid id) =>
            BuildMatchDetailResponseAsync(id, requireActive: true);

        /// <summary>
        /// Misma carga que la vista pública, sin filtrar por <see cref="Match.IsActive"/>:
        /// la mesa debe ver el partido aunque el flag o datos de vitrina fallen, y evita 404 al transmitir.
        /// </summary>
        [HttpGet("{id:guid}/mesa-detail")]
        [Authorize(Policy = "tourn.mesa.detail")]
        public Task<IActionResult> GetMatchDetailForMesa(Guid id) =>
            BuildMatchDetailResponseAsync(id, requireActive: false);

        private async Task<IActionResult> BuildMatchDetailResponseAsync(Guid id, bool requireActive)
        {
            var match = await _context.Matches
                .AsNoTracking()
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .Include(m => m.Discipline)
                .Include(m => m.Phase)
                    .ThenInclude(p => p.Competition)
                        .ThenInclude(c => c.Tournament)
                .Include(m => m.Phase)
                    .ThenInclude(p => p.Competition)
                        .ThenInclude(c => c.Discipline)
                            .ThenInclude(d => d.Rules)
                .Include(m => m.Venue)
                .FirstOrDefaultAsync(m => m.Id == id && (!requireActive || m.IsActive));

            if (match == null) return NotFound();

            // Vitrina pública: solo partidos con ambos equipos. Mesa: permite slots sin rival aún (llaves / fixture TBD).
            if (requireActive && (match.LocalTeam == null || match.VisitorTeam == null))
                return NotFound("Partido sin equipos asignados.");

            if (match.Phase?.Competition == null)
                return NotFound("Partido sin competencia asociada.");

            var comp = match.Phase.Competition;
            await AutoLockRostersForMatchDetailAsync(match.Id);
            var tournament = comp.Tournament;
            if (tournament == null)
                return NotFound("Torneo no disponible para este partido.");

            var sportRules = await _sportRulesBuilder.BuildMapAsync(
                comp.Id,
                comp.Discipline?.Rules,
                comp.Discipline?.TemplateKey);

            await RepairEnVivoKickoffAsync(id, match.Status, match.LocalTeamId);
            await EnsureLiveClockAnchorForDetailAsync(id, match.Status, match.LocalTeamId);

            var clockRow = await _context.Matches.AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new { m.ClockAccumulatedSeconds, m.ClockPeriodAnchorUtc })
                .FirstOrDefaultAsync();

            var events = await _context.MatchEvents
                .AsNoTracking()
                .Where(e => e.MatchId == id)
                .OrderBy(e => e.OccurredAt == null ? 1 : 0)
                .ThenBy(e => e.OccurredAt)
                .ThenBy(e => e.Period)
                .ThenBy(e => e.Minute)
                .ThenBy(e => e.Id)
                .Select(e => new MatchEventDetailDto
                {
                    Id = e.Id,
                    Minute = e.Minute,
                    Type = e.Type.ToString(),
                    TeamId = e.TeamId,
                    TeamName = _context.Teams.Where(t => t.Id == e.TeamId).Select(t => t.Name).FirstOrDefault() ?? "Equipo",
                    PlayerId = e.PlayerId,
                    PlayerName = e.Player != null ? e.Player.Name : null,
                    RelatedPlayerId = e.RelatedPlayerId,
                    RelatedPlayerName = e.RelatedPlayer != null ? e.RelatedPlayer.Name : null,
                    Note = e.Note,
                    Value = e.Value,
                    Period = e.Period,
                    OccurredAt = e.OccurredAt
                })
                .ToListAsync();

            var lineups = await _context.MatchLineups
                .AsNoTracking()
                .Where(l => l.MatchId == id)
                .Include(l => l.Team)
                .Include(l => l.Players)
                    .ThenInclude(p => p.Player)
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
                            Role = p.Role.ToString(),
                            Number = p.ShirtNumber ?? p.Player.Number,
                            Position = p.Position.ToString(),
                            p.IsCaptain,
                            p.IsGoalkeeper,
                            p.Observation
                        })
                })
                .ToListAsync();

            var localRosterLocked = false;
            var visitorRosterLocked = false;
            if (match.LocalTeamId.HasValue || match.VisitorTeamId.HasValue)
            {
                var teamIds = new[] { match.LocalTeamId, match.VisitorTeamId }
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();
                var lockRows = await _context.CompetitionTeams
                    .AsNoTracking()
                    .Where(ct => ct.CompetitionId == comp.Id && teamIds.Contains(ct.TeamId))
                    .Select(ct => new { ct.TeamId, ct.RosterLocked })
                    .ToListAsync();
                localRosterLocked = match.LocalTeamId.HasValue
                    && lockRows.FirstOrDefault(r => r.TeamId == match.LocalTeamId.Value)?.RosterLocked == true;
                visitorRosterLocked = match.VisitorTeamId.HasValue
                    && lockRows.FirstOrDefault(r => r.TeamId == match.VisitorTeamId.Value)?.RosterLocked == true;
            }

            var canSubmitLocalLineup = false;
            var canSubmitVisitorLineup = false;
            string? localLineupDelegate = null;
            string? visitorLineupDelegate = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (match.LocalTeamId.HasValue)
                {
                    canSubmitLocalLineup = await TeamManagementAuthorization.CanSubmitMatchLineupAsync(
                        User, _context, match.LocalTeamId.Value);
                    localLineupDelegate = await ResolveLineupDelegateLabelAsync(match.LocalTeamId.Value);
                }
                if (match.VisitorTeamId.HasValue)
                {
                    canSubmitVisitorLineup = await TeamManagementAuthorization.CanSubmitMatchLineupAsync(
                        User, _context, match.VisitorTeamId.Value);
                    visitorLineupDelegate = await ResolveLineupDelegateLabelAsync(match.VisitorTeamId.Value);
                }
            }

            var disciplineTitle = comp.Discipline?.Name ?? match.Discipline?.Name ?? "Deporte";
            var competitionLabel = string.IsNullOrWhiteSpace(comp.CategoryName)
                ? disciplineTitle
                : $"{disciplineTitle} · {comp.CategoryName}";

            return Ok(new
            {
                match.Id,
                Status = match.Status.ToString(),
                match.ScheduledAt,
                match.LocalScore,
                match.VisitorScore,
                match.LocalTeamId,
                match.VisitorTeamId,
                LocalPenaltyScore = match.LocalPenaltyScore ?? 0,
                VisitorPenaltyScore = match.VisitorPenaltyScore ?? 0,
                PhaseIsDirectElimination = match.Phase?.IsDirectElimination ?? false,
                PhaseIsDoubleLeg = match.Phase?.IsDoubleLeg ?? false,
                LocalTeamName = match.LocalTeam?.Name ?? "Por asignar",
                LocalTeamLogo = match.LocalTeam?.LogoUrl,
                VisitorTeamName = match.VisitorTeam?.Name ?? "Por asignar",
                VisitorTeamLogo = match.VisitorTeam?.LogoUrl,
                CanSubmitLocalLineup = canSubmitLocalLineup,
                CanSubmitVisitorLineup = canSubmitVisitorLineup,
                LocalLineupDelegate = localLineupDelegate,
                VisitorLineupDelegate = visitorLineupDelegate,
                LocalRosterLocked = localRosterLocked,
                VisitorRosterLocked = visitorRosterLocked,
                LocalLineupOpenUntilUtc = match.LocalLineupOpenUntilUtc,
                VisitorLineupOpenUntilUtc = match.VisitorLineupOpenUntilUtc,
                DisciplineName = match.Discipline?.Name ?? disciplineTitle,
                PhaseName = match.Phase?.Name ?? "",
                Gender = comp.Gender.ToString(),
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                TournamentYear = tournament.Year,
                TournamentStatus = (int)tournament.Status,
                TournamentStatusName = tournament.Status.ToString(),
                CompetitionId = comp.Id,
                CompetitionLabel = competitionLabel,
                DisciplineTemplateKey = comp.Discipline?.TemplateKey,
                SportRules = sportRules,
                match.VenueId,
                VenueName = match.Venue != null ? match.Venue.Name : null,
                ClockAccumulatedSeconds = clockRow?.ClockAccumulatedSeconds ?? match.ClockAccumulatedSeconds,
                ClockPeriodAnchorUtc = clockRow?.ClockPeriodAnchorUtc ?? match.ClockPeriodAnchorUtc,
                ClockWidgetKind = match.ClockWidgetKind.ToString(),
                BroadcastWidgetJson = _broadcastWidgetStore.GetSnapshotJson(id),
                Events = events,
                Lineups = lineups
            });
        }

        // 1. Ver partidos de una fecha específica (Lo que verá la "Mesa")
        [HttpGet("journal/{journalId}")]
        public async Task<IActionResult> GetByJournal(Guid journalId)
        {
            var matches = await _context.Matches
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .Include(m => m.Venue)
                .Where(m => m.JournalId == journalId)
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();

            return Ok(matches);
        }

        [HttpGet("{id}/result")]
        public async Task<IActionResult> GetMatchResult(Guid id)
        {
            var match = await _context.Matches
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            var result = new MatchDetailDto
            {
                Id = match.Id,
                LocalTeamName = match.LocalTeam?.Name ?? "—",
                LocalTeamLogo = match.LocalTeam?.LogoUrl,
                LocalScore = match.LocalScore,
                VisitorTeamName = match.VisitorTeam?.Name ?? "—",
                VisitorTeamLogo = match.VisitorTeam?.LogoUrl,
                VisitorScore = match.VisitorScore,
                Status = match.Status.ToString(),
                ScheduledAt = match.ScheduledAt
            };

            return Ok(result);
        }
        [HttpGet("{id}/timeline")]
        public async Task<IActionResult> GetMatchTimeline(Guid id)
        {
            var events = await _context.MatchEvents
                .AsNoTracking()
                .Where(e => e.MatchId == id)
                .OrderBy(e => e.OccurredAt == null ? 1 : 0)
                .ThenByDescending(e => e.OccurredAt)
                .ThenByDescending(e => e.Period)
                .ThenByDescending(e => e.Minute)
                .Select(e => new MatchEventDetailDto
                {
                    Id = e.Id,
                    Minute = e.Minute,
                    Type = e.Type.ToString(),
                    TeamId = e.TeamId,
                    TeamName = _context.Teams.Where(t => t.Id == e.TeamId).Select(t => t.Name).FirstOrDefault() ?? "Equipo",
                    PlayerId = e.PlayerId,
                    PlayerName = e.Player != null ? e.Player.Name : null,
                    RelatedPlayerId = e.RelatedPlayerId,
                    RelatedPlayerName = e.RelatedPlayer != null ? e.RelatedPlayer.Name : null,
                    Note = e.Note,
                    Value = e.Value,
                    Period = e.Period,
                    OccurredAt = e.OccurredAt
                })
                .ToListAsync();

            return Ok(events);
        }

        [HttpGet("{id}/report")]
        [Authorize(Policy = Permissions.TournMatchReportDownload)]
        public async Task<IActionResult> GetMatchReport(Guid id)
        {
            var report = await BuildMatchReportAsync(id);
            return report == null ? NotFound("Partido no encontrado.") : Ok(report);
        }

        [HttpGet("{id}/report.csv")]
        [Authorize(Policy = Permissions.TournMatchReportDownload)]
        public async Task<IActionResult> DownloadMatchReportCsv(Guid id)
        {
            var report = await BuildMatchReportAsync(id);
            if (report == null) return NotFound("Partido no encontrado.");

            var sb = new StringBuilder();
            sb.AppendLine($"Partido,{Csv(report.TournamentName ?? "")},{Csv(report.CompetitionName ?? "")}");
            sb.AppendLine($"Marcador final,{report.LocalScore},{report.VisitorScore}");
            sb.AppendLine($"Definición,{Csv(report.DecisionType ?? "No definida")}");
            sb.AppendLine($"Penales,{report.LocalPenaltyScore},{report.VisitorPenaltyScore}");
            sb.AppendLine();
            sb.AppendLine("Equipo,N°,Jugador,Condición,Goles,Amarillas,Segunda amarilla,Roja directa,Roja doble amarilla,Cambios sale,Cambios entra,Observación");
            foreach (var team in report.Teams)
            {
                foreach (var p in team.Players)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                        Csv(team.TeamName),
                        Csv(p.Number?.ToString() ?? ""),
                        Csv(p.PlayerName),
                        Csv(p.Role),
                        p.Goals.ToString(),
                        p.YellowCards.ToString(),
                        p.SecondYellowCards.ToString(),
                        p.DirectRedCards.ToString(),
                        p.DoubleYellowRedCards.ToString(),
                        p.SubstitutionsOut.ToString(),
                        p.SubstitutionsIn.ToString(),
                        Csv(p.Observation ?? "")
                    }));
                }

                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(team.TeamName),
                    "",
                    Csv("RESUMEN"),
                    "",
                    team.TotalGoals.ToString(),
                    team.TotalYellowCards.ToString(),
                    team.TotalSecondYellowCards.ToString(),
                    team.TotalDirectRedCards.ToString(),
                    team.TotalDoubleYellowRedCards.ToString(),
                    team.TotalSubstitutionsOut.ToString(),
                    team.TotalSubstitutionsIn.ToString(),
                    Csv($"Titulares: {team.StartersCount}; Suplentes: {team.SubstitutesCount}")
                }));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"acta-partido-{id}.csv");
        }

        [HttpGet("{id}/report.pdf")]
        [Authorize(Policy = Permissions.TournMatchReportDownload)]
        public async Task<IActionResult> DownloadMatchReportPdf(Guid id)
        {
            var report = await BuildMatchReportAsync(id);
            if (report == null) return NotFound("Partido no encontrado.");
            var pdf = MatchActaPdfComposer.Generate(report);
            return File(pdf, "application/pdf", $"acta-partido-{id}.pdf");
        }

        // 3. Cambiar estado (mesa / transmisión: En vivo, programado, suspendido, etc.)
        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] PatchMatchStatusDto dto)
        {
            var match = await _context.Matches
                .Include(m => m.Phase)
                    .ThenInclude(p => p.Competition)
                        .ThenInclude(c => c.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();

            var tourn = match.Phase?.Competition?.Tournament;
            if (dto.Status == MatchStatus.EnVivo
                && (tourn == null || tourn.Status != TournamentStatus.Activo))
            {
                return BadRequest(new
                {
                    message = "Solo se puede poner un partido en vivo cuando el torneo está Activo (en competencia)."
                });
            }

            var previousStatus = match.Status;

            // La transmisión (En vivo / Programado / Suspendido) no congela el cronómetro: eso es PATCH /clock.

            match.Status = dto.Status;

            // Primer arranque en vivo: registrar inicio del 1.º periodo para el cronómetro (TeamId = local).
            // El cronómetro del cliente usa OccurredAt del InicioPeriodo, no ScheduledAt: si el inicio ya existía
            // con fecha vieja u otro día, al entrar en vivo hay que alinearlo al momento real del clic (salvo reanudar el mismo día).
            if (dto.Status == MatchStatus.EnVivo && match.LocalTeamId.HasValue)
            {
                var yaHayInicio = await _context.MatchEvents
                    .AnyAsync(e => e.MatchId == match.Id && e.Type == MatchEventType.InicioPeriodo);
                if (!yaHayInicio)
                {
                    _context.MatchEvents.Add(new MatchEvent
                    {
                        MatchId = match.Id,
                        Minute = 0,
                        Period = 1,
                        Type = MatchEventType.InicioPeriodo,
                        TeamId = match.LocalTeamId.Value,
                        OccurredAt = DateTime.UtcNow,
                        Value = 0
                    });
                }
                else if (previousStatus != MatchStatus.EnVivo)
                {
                    await AlignKickoffOccurredAtOnGoLiveAsync(match.Id);
                }
            }

            if (dto.Status == MatchStatus.EnVivo
                && previousStatus != MatchStatus.EnVivo
                && match.ClockPeriodAnchorUtc == null)
            {
                match.ClockPeriodAnchorUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Cualquier cambio de estado (En vivo, pausa de transmisión, etc.): mismo snapshot para mesa y público.
            await _hubContext.Clients.Group(MatchRoomGroup(match.Id))
                .SendAsync("ReceiveMatchUpdate", HubMatchSnapshot(match));

            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(match);
        }

        /// <summary>
        /// Mesa: registrar goles de la tanda de penales cuando la eliminatoria (ida simple) queda empatada en el marcador global.
        /// </summary>
        [HttpPatch("{id}/penalty-score")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> PatchPenaltyScore(Guid id, [FromBody] PatchPenaltyScoreDto dto)
        {
            if (dto.LocalPenaltyScore < 0 || dto.VisitorPenaltyScore < 0)
                return BadRequest(new { message = "Los penales no pueden ser negativos." });
            if (dto.LocalPenaltyScore > 99 || dto.VisitorPenaltyScore > 99)
                return BadRequest(new { message = "Revisá los valores de la tanda de penales." });

            var match = await _context.Matches
                .Include(m => m.Phase)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return NotFound();
            if (match.Status == MatchStatus.Finalizado)
                return BadRequest(new { message = "El partido ya está finalizado." });
            if (match.Phase?.IsDirectElimination != true)
                return BadRequest(new { message = "Solo en fase eliminatoria se registra la tanda de penales." });
            if (match.Phase?.IsDoubleLeg == true)
                return BadRequest(new { message = "En ida y vuelta el desempate no se registra por penales en este mismo partido." });
            if (match.LocalScore != match.VisitorScore)
                return BadRequest(new { message = "Penales solo si el marcador global está empatado." });

            match.LocalPenaltyScore = dto.LocalPenaltyScore;
            match.VisitorPenaltyScore = dto.VisitorPenaltyScore;
            await _context.SaveChangesAsync();
            await PushBroadcastAfterActaAsync(id, match, new { type = "PENALTY_SCORE", message = "Penales actualizados." });
            return Ok(new { localPenaltyScore = match.LocalPenaltyScore, visitorPenaltyScore = match.VisitorPenaltyScore });
        }

        // 4. Asignar sede y hora (Programación)
        [HttpPatch("{id}/schedule")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> ScheduleMatch(Guid id, [FromBody] DateTime scheduledAt, [FromQuery] Guid venueId)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            // Hora de reloj del evento (sin forzar UTC): el cliente envía "yyyy-MM-ddTHH:mm:ss" local a la organización.
            match.ScheduledAt = DateTime.SpecifyKind(scheduledAt, DateTimeKind.Unspecified);
            match.VenueId = venueId;

            // Si se reprograma a un horario con margen, reabre bloqueo automático de listas para este partido.
            if (match.Status != MatchStatus.EnVivo && match.Status != MatchStatus.Finalizado)
            {
                var scheduledUtc = DateTime.SpecifyKind(match.ScheduledAt, DateTimeKind.Local).ToUniversalTime();
                var lockMinutesBeforeMatch = await ResolveLineupCloseMinutesAsync(match.DisciplineId);
                if (scheduledUtc > DateTime.UtcNow.AddMinutes(lockMinutesBeforeMatch) && match.PhaseId != Guid.Empty)
                {
                    var competitionId = await _context.Phases
                        .Where(p => p.Id == match.PhaseId)
                        .Select(p => (Guid?)p.CompetitionId)
                        .FirstOrDefaultAsync();
                    if (competitionId == null)
                    {
                        await _context.SaveChangesAsync();
                        await _vitrina.NotifyLandingRefreshAsync();
                        return Ok(match);
                    }

                    var teamIds = new[] { match.LocalTeamId, match.VisitorTeamId }
                        .Where(x => x.HasValue)
                        .Select(x => x!.Value)
                        .ToList();
                    if (teamIds.Count > 0)
                    {
                        var rows = await _context.CompetitionTeams
                            .Where(ct => ct.CompetitionId == competitionId.Value
                                && teamIds.Contains(ct.TeamId)
                                && ct.RosterLocked)
                            .ToListAsync();
                        foreach (var row in rows)
                        {
                            row.RosterLocked = false;
                            row.RosterUnlockedAt = DateTime.UtcNow;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(match);
        }

        /// <summary>
        /// Registrar un evento en el partido (gol, tarjeta, sustitución, etc.) y actualizar el marcador si es necesario.
        /// ⚽ Este endpoint es crucial para el seguimiento en tiempo real del partido. Asegúrate de enviar el evento 
        /// correcto con el TeamId correspondiente para que el marcador se actualice automáticamente.
        /// </summary>
        /// <param name="id">ID del partido</param>
        /// <param name="dto">Datos del evento</param>
        /// <returns>Resultado de la operación</returns>
        [HttpPost("{id}/events")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> AddEvent(Guid id, [FromBody] MatchEventDto dto)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound("Partido no encontrado");

            if (dto.TeamId != match.LocalTeamId && dto.TeamId != match.VisitorTeamId)
                return BadRequest("El TeamId no pertenece a este partido.");

            if (dto.Type == MatchEventType.Sustitucion)
            {
                if (!await PlayersBelongToTeamAsync(dto.TeamId, dto.PlayerId, dto.RelatedPlayerId))
                    return BadRequest("En sustitución, ambos jugadores deben pertenecer al equipo elegido.");
            }
            else if (dto.PlayerId.HasValue)
            {
                if (!await PlayersBelongToTeamAsync(dto.TeamId, dto.PlayerId, null))
                    return BadRequest("El jugador debe pertenecer al equipo del evento.");
            }

            var playerValidation = await ValidatePlayersForMatchActaAsync(match, dto.TeamId, dto.PlayerId, dto.RelatedPlayerId);
            if (playerValidation != null)
                return BadRequest(playerValidation);

            // ⚽ 1. Lógica de Marcador ÚNICA y VALIDADA
            if (dto.Type == MatchEventType.Goal || dto.Type == MatchEventType.Puntaje)
            {
                if (match.LocalTeamId == dto.TeamId)
                    match.LocalScore += dto.Value;
                else if (match.VisitorTeamId == dto.TeamId)
                    match.VisitorScore += dto.Value;
                else
                    return BadRequest("El TeamId enviado no pertenece a este partido.");
            }
            else if (dto.Type == MatchEventType.PenaltyGoal)
            {
                if (match.LocalTeamId == dto.TeamId) match.LocalPenaltyScore += 1;
                else if (match.VisitorTeamId == dto.TeamId) match.VisitorPenaltyScore += 1;
            }

            if (dto.Type == MatchEventType.FinPeriodo)
            {
                match.ClockAccumulatedSeconds = 0;
                match.ClockPeriodAnchorUtc = null;
            }
            else if (dto.Type == MatchEventType.InicioPeriodo)
            {
                match.ClockAccumulatedSeconds = 0;
                match.ClockPeriodAnchorUtc = match.Status == MatchStatus.EnVivo ? DateTime.UtcNow : null;
            }

            // 2. Creamos el evento
            var newEvent = new MatchEvent
            {
                MatchId = id,
                Minute = dto.Minute,
                Type = dto.Type,
                TeamId = dto.TeamId,
                PlayerId = dto.PlayerId,
                RelatedPlayerId = dto.RelatedPlayerId,
                Note = dto.Note,
                Value = dto.Value,
                Period = dto.Period,
                OccurredAt = DateTime.UtcNow
            };

            _context.MatchEvents.Add(newEvent);

            // 💾 GUARDADO CRÍTICO: Aquí se guarda el evento Y el nuevo score del partido
            await _context.SaveChangesAsync();

            await PushBroadcastAfterActaAsync(id, match, new
            {
                minute = dto.Minute,
                type = dto.Type.ToString(),
                teamId = dto.TeamId,
                note = dto.Note
            });

            return Ok(new { message = "Evento y marcador actualizados", score = $"{match.LocalScore}-{match.VisitorScore}" });
        }


        [HttpPatch("events/{eventId}/player")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> UpdateEventPlayer(Guid eventId, [FromBody] Guid? playerId)
        {
            // 1. Buscamos el evento
            var matchEvent = await _context.MatchEvents
                .Include(m => m.Match)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (matchEvent == null) return NotFound("Evento no encontrado.");

            // 2. Actualizamos solo el jugador
            matchEvent.PlayerId = playerId;
            await _context.SaveChangesAsync();

            // 3. ¡IMPORTANTE! Obtenemos el nombre del jugador para SignalR
            var playerName = "Jugador no informado";
            if (playerId.HasValue)
            {
                playerName = await _context.Players
                    .Where(p => p.Id == playerId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync() ?? "Jugador desconocido";
            }

            // 🚀 4. Avisamos por SignalR que el gol ya tiene nombre
            await _hubContext.Clients.Group(matchEvent.MatchId.ToString().ToLower()).SendAsync("ReceiveEventUpdate", new
            {
                eventId = eventId,
                matchId = matchEvent.MatchId,
                playerId = playerId,
                playerName = playerName,
                message = $"¡Autor confirmado! Gol de {playerName}"
            });

            await PushBroadcastAfterActaAsync(matchEvent.MatchId, matchEvent.Match);

            return Ok(new { message = "Jugador actualizado", playerName });
        }

        /// <summary>
        /// Corregir jugador/es o nota de un evento (campos enviados en el JSON; omitir una clave para no cambiarla).
        /// </summary>
        [HttpPatch("events/{eventId}")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> PatchEvent(Guid eventId, [FromBody] JsonElement body)
        {
            var matchEvent = await _context.MatchEvents
                .Include(e => e.Match)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (matchEvent == null) return NotFound("Evento no encontrado.");
            if (body.ValueKind != JsonValueKind.Object) return BadRequest("Cuerpo inválido.");

            if (body.TryGetProperty("playerId", out var jp))
            {
                matchEvent.PlayerId = jp.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                    ? null
                    : jp.GetGuid();
            }

            if (body.TryGetProperty("relatedPlayerId", out var jr))
            {
                matchEvent.RelatedPlayerId = jr.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                    ? null
                    : jr.GetGuid();
            }

            if (body.TryGetProperty("note", out var jn))
                matchEvent.Note = jn.ValueKind == JsonValueKind.Null ? null : jn.GetString();

            if (matchEvent.Type != MatchEventType.Sustitucion && matchEvent.RelatedPlayerId.HasValue)
                return BadRequest("RelatedPlayerId solo aplica a eventos de sustitución.");

            if (matchEvent.Type == MatchEventType.Sustitucion)
            {
                if (!await PlayersBelongToTeamAsync(matchEvent.TeamId, matchEvent.PlayerId, matchEvent.RelatedPlayerId))
                    return BadRequest("Los jugadores deben pertenecer al equipo del evento.");
            }
            else if (matchEvent.PlayerId.HasValue
                     && !await PlayersBelongToTeamAsync(matchEvent.TeamId, matchEvent.PlayerId, null))
            {
                return BadRequest("El jugador debe pertenecer al equipo del evento.");
            }

            await _context.SaveChangesAsync();
            await PushBroadcastAfterActaAsync(matchEvent.MatchId, matchEvent.Match);
            return Ok(new { message = "Evento actualizado" });
        }

        [HttpDelete("events/{eventId}")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> DeleteEvent(Guid eventId)
        {
            // 1. Buscar el evento incluyendo el partido relacionado
            var matchEvent = await _context.MatchEvents
                .Include(e => e.Match)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (matchEvent == null) return NotFound("El evento no existe.");

            var match = matchEvent.Match;

            // 2. Si el evento era un Gol o Puntaje, debemos REVERTIR el marcador
            if (matchEvent.Type == MatchEventType.Goal || matchEvent.Type == MatchEventType.Puntaje)
            {
                if (match.LocalTeamId == matchEvent.TeamId)
                    match.LocalScore -= matchEvent.Value;
                else
                    match.VisitorScore -= matchEvent.Value;

                // Aseguramos que el marcador no sea negativo (por si acaso)
                if (match.LocalScore < 0) match.LocalScore = 0;
                if (match.VisitorScore < 0) match.VisitorScore = 0;
            }
            else if (matchEvent.Type == MatchEventType.PenaltyGoal)
            {
                if (match.LocalTeamId == matchEvent.TeamId)
                    match.LocalPenaltyScore = Math.Max(0, (match.LocalPenaltyScore ?? 0) - 1);
                else if (match.VisitorTeamId == matchEvent.TeamId)
                    match.VisitorPenaltyScore = Math.Max(0, (match.VisitorPenaltyScore ?? 0) - 1);
            }

            // 3. Eliminar el evento de la base de datos
            _context.MatchEvents.Remove(matchEvent);
            await _context.SaveChangesAsync();

            await PushBroadcastAfterActaAsync(match.Id, match, new
            {
                type = "EVENT_DELETED",
                message = "Un evento fue anulado. El marcador se ha actualizado.",
                deletedEventId = eventId
            });

            return Ok(new { message = "Evento eliminado y marcador corregido", localScore = match.LocalScore, visitorScore = match.VisitorScore });
        }

        [HttpPatch("{id}/finish")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> FinishMatch(Guid id)
        {
            // 1. Incluimos Phase para saber si es la Final y los Teams para el nombre/logo
            var match = await _context.Matches
                .Include(m => m.Phase)
                .Include(m => m.Journal)
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound("El partido no existe.");
            if (match.Status == MatchStatus.Finalizado)
                return BadRequest("El partido ya fue finalizado previamente.");

            if (match.Phase?.IsDirectElimination == true
                && match.Phase.IsDoubleLeg == false
                && match.LocalTeamId.HasValue
                && match.VisitorTeamId.HasValue
                && match.LocalScore == match.VisitorScore)
            {
                var lp = match.LocalPenaltyScore ?? 0;
                var vp = match.VisitorPenaltyScore ?? 0;
                if (lp == vp)
                {
                    return BadRequest(new
                    {
                        message =
                            "Eliminatoria empatada: registrá la tanda de penales (mesa, sección «Penales») con un ganador, " +
                            "o desempatá en suplementario sumando goles al marcador antes de finalizar."
                    });
                }
            }

            if (match.Status == MatchStatus.EnVivo)
                MatchChronometerShared.FlushRunningClockSegment(match);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🏆 LÓGICA DE GANADOR (Mantenemos tu lógica actual)
                if (match.LocalScore > match.VisitorScore)
                    match.WinnerId = match.LocalTeamId;
                else if (match.VisitorScore > match.LocalScore)
                    match.WinnerId = match.VisitorTeamId;
                else
                {
                    if (match.LocalPenaltyScore > match.VisitorPenaltyScore)
                        match.WinnerId = match.LocalTeamId;
                    else if (match.VisitorPenaltyScore > match.LocalPenaltyScore)
                        match.WinnerId = match.VisitorTeamId;
                }

                match.Status = MatchStatus.Finalizado;
                await _context.SaveChangesAsync();

                // PERSISTENCIA DE ESTADÍSTICAS... (tu código de Standings se mantiene igual)
                if (match.GroupId.HasValue)
                {
                    await _standingsService.UpdateGroupStandingsAsync(match.GroupId.Value);
                    // ... (notificación de standings)
                }

                if (match.WinnerId.HasValue && match.Phase?.IsDirectElimination == true)
                {
                    await AutoAdvanceKnockoutWinnerAsync(match);
                    await _context.SaveChangesAsync();
                }

                var championAssigned = await TryAssignCompetitionChampionAsync(match);
                if (championAssigned)
                    await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                if (championAssigned && match.WinnerId.HasValue && match.Phase != null)
                {
                    var championTeam = match.WinnerId == match.LocalTeamId ? match.LocalTeam : match.VisitorTeam;
                    if (championTeam != null)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceiveChampion", new
                        {
                            competitionId = match.Phase.CompetitionId,
                            championName = championTeam.Name,
                            championLogo = championTeam.LogoUrl,
                            score = $"{match.LocalScore} - {match.VisitorScore}",
                            message = $"¡Felicidades {championTeam.Name}! Campeón de la competencia."
                        });
                    }
                }

                // SignalR estándar de fin de partido
                await _hubContext.Clients.Group(MatchRoomGroup(id)).SendAsync("ReceiveMatchUpdate", new
                {
                    matchId = id,
                    status = "Finalizado",
                    winnerId = match.WinnerId,
                    finalScore = $"{match.LocalScore} - {match.VisitorScore}",
                    localScore = match.LocalScore,
                    visitorScore = match.VisitorScore,
                    clockAccumulatedSeconds = match.ClockAccumulatedSeconds,
                    clockPeriodAnchorUtc = match.ClockPeriodAnchorUtc,
                    clockWidgetKind = match.ClockWidgetKind.ToString(),
                });

                await _vitrina.NotifyLandingRefreshAsync();

                return Ok(new
                {
                    message = "Partido finalizado.",
                    winnerId = match.WinnerId,
                    competitionChampionAssigned = championAssigned
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Al pasar a En vivo desde otro estado: si el evento <see cref="MatchEventType.InicioPeriodo"/> activo tiene
        /// <see cref="MatchEvent.OccurredAt"/> nulo o de otro día / muy antiguo, lo alinea a UTC actual
        /// para que el cronómetro refleje el momento real del arranque (p. ej. programado 14:00 pero salió 14:03).
        /// No toca reanudaciones el mismo día con marca reciente (pausa corta de transmisión).
        /// </summary>
        private static bool ShouldRefreshKickoffOccurredAt(DateTime? occurredAt, DateTime utcNow)
        {
            if (!occurredAt.HasValue) return true;
            var o = occurredAt.Value;
            if (o.Kind == DateTimeKind.Unspecified)
                o = DateTime.SpecifyKind(o, DateTimeKind.Utc);
            else if (o.Kind == DateTimeKind.Local)
                o = o.ToUniversalTime();

            if ((utcNow - o).TotalHours >= 20)
                return true;
            if (o.Date != utcNow.Date)
                return true;
            return false;
        }

        private async Task AutoAdvanceKnockoutWinnerAsync(Match match)
        {
            if (match.Journal == null || !match.WinnerId.HasValue) return;

            var currentSeq = match.Journal.Sequence;
            var nextJournal = await _context.Journals
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.PhaseId == match.PhaseId && j.Sequence == currentSeq + 1);
            if (nextJournal == null) return; // Ya es la final

            var currentRoundIds = await _context.Matches
                .Where(m => m.JournalId == match.JournalId && m.IsActive)
                .OrderBy(m => m.CreatedAt)
                .Select(m => m.Id)
                .ToListAsync();
            var idx = currentRoundIds.FindIndex(x => x == match.Id);
            if (idx < 0) return;

            var nextRound = await _context.Matches
                .Where(m => m.JournalId == nextJournal.Id && m.IsActive)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            if (nextRound.Count == 0) return;

            var targetIndex = idx / 2;
            if (targetIndex < 0 || targetIndex >= nextRound.Count) return;
            var target = nextRound[targetIndex];
            var asLocal = (idx % 2) == 0;

            if (asLocal) target.LocalTeamId = match.WinnerId;
            else target.VisitorTeamId = match.WinnerId;
        }

        /// <summary>
        /// Si el partido cerrado es el único partido activo de la última jornada de una fase de eliminatoria ida simple,
        /// persiste <see cref="Competition.ChampionTeamId"/> (p. ej. final única). Si la última jornada tiene más de un
        /// partido (final + 3.er puesto), no asigna: usar PATCH manual en la competencia.
        /// </summary>
        private async Task<bool> TryAssignCompetitionChampionAsync(Match finishedMatch)
        {
            if (finishedMatch.WinnerId is null || finishedMatch.Phase is null)
                return false;
            if (!finishedMatch.Phase.IsDirectElimination || finishedMatch.Phase.IsDoubleLeg)
                return false;
            if (finishedMatch.Journal == null)
                return false;

            var phaseId = finishedMatch.PhaseId;
            var maxSeq = await _context.Journals.AsNoTracking()
                .Where(j => j.PhaseId == phaseId)
                .MaxAsync(j => (int?)j.Sequence) ?? 0;
            if (finishedMatch.Journal.Sequence != maxSeq)
                return false;

            var lastJournalIds = await _context.Journals.AsNoTracking()
                .Where(j => j.PhaseId == phaseId && j.Sequence == maxSeq)
                .Select(j => j.Id)
                .ToListAsync();

            var activeIds = await _context.Matches.AsNoTracking()
                .Where(m => lastJournalIds.Contains(m.JournalId) && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync();
            if (activeIds.Count != 1 || activeIds[0] != finishedMatch.Id)
                return false;

            var competitionId = finishedMatch.Phase.CompetitionId;
            var comp = await _context.Competitions.FirstOrDefaultAsync(c => c.Id == competitionId);
            if (comp == null)
                return false;

            comp.ChampionTeamId = finishedMatch.WinnerId;
            comp.ChampionDecidedAtUtc = DateTime.UtcNow;
            return true;
        }

        private async Task AlignKickoffOccurredAtOnGoLiveAsync(Guid matchId)
        {
            var marks = await _context.MatchEvents
                .Where(e => e.MatchId == matchId &&
                            (e.Type == MatchEventType.InicioPeriodo || e.Type == MatchEventType.FinPeriodo))
                .OrderBy(e => e.OccurredAt.HasValue ? 0 : 1)
                .ThenBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .ToListAsync();

            int? activePeriod = null;
            var inPlay = false;
            MatchEvent? lastInicioForActive = null;

            foreach (var ev in marks)
            {
                if (ev.Type == MatchEventType.InicioPeriodo)
                {
                    var p = MatchChronometerShared.NormInicioPeriod(ev);
                    activePeriod = p;
                    inPlay = true;
                    lastInicioForActive = ev;
                }
                else if (ev.Type == MatchEventType.FinPeriodo)
                {
                    var pFin = ev.Period > 0 ? ev.Period : activePeriod ?? 1;
                    if (activePeriod.HasValue && pFin == activePeriod.Value)
                    {
                        inPlay = false;
                        lastInicioForActive = null;
                    }
                }
            }

            if (!inPlay || lastInicioForActive == null)
                return;

            var now = DateTime.UtcNow;
            if (!ShouldRefreshKickoffOccurredAt(lastInicioForActive.OccurredAt, now))
                return;

            lastInicioForActive.OccurredAt = now;

            var matchEntity = await _context.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
            if (matchEntity != null)
            {
                matchEntity.ClockAccumulatedSeconds = 0;
                matchEntity.ClockPeriodAnchorUtc = now;
            }
        }

        /// <summary>
        /// Partido en vivo sin marca de tiempo en el inicio de periodo: el cronómetro queda en 0:00.
        /// Crea el 1T si falta o asigna <see cref="MatchEvent.OccurredAt"/> al primer inicio que lo necesite.
        /// </summary>
        /// <summary>
        /// Partido en vivo en juego sin ancla de cronómetro (p. ej. activado antes del despliegue del reloj):
        /// persiste <see cref="Match.ClockPeriodAnchorUtc"/> para que el cliente pueda calcular el transcurrido.
        /// En descanso entre tiempos no asigna ancla.
        /// </summary>
        private async Task EnsureLiveClockAnchorForDetailAsync(Guid matchId, MatchStatus status, Guid? localTeamId)
        {
            if (status != MatchStatus.EnVivo || !localTeamId.HasValue)
                return;

            var tracked = await _context.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
            if (tracked == null || tracked.ClockPeriodAnchorUtc.HasValue)
                return;

            var marks = await _context.MatchEvents
                .Where(e => e.MatchId == matchId &&
                            (e.Type == MatchEventType.InicioPeriodo || e.Type == MatchEventType.FinPeriodo))
                .OrderBy(e => e.OccurredAt.HasValue ? 0 : 1)
                .ThenBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .ToListAsync();

            int? activePeriod = null;
            var inPlay = false;
            MatchEvent? lastInicioForActive = null;

            foreach (var ev in marks)
            {
                if (ev.Type == MatchEventType.InicioPeriodo)
                {
                    var p = MatchChronometerShared.NormInicioPeriod(ev);
                    activePeriod = p;
                    inPlay = true;
                    lastInicioForActive = ev;
                }
                else if (ev.Type == MatchEventType.FinPeriodo)
                {
                    var pFin = ev.Period > 0 ? ev.Period : activePeriod ?? 1;
                    if (activePeriod.HasValue && pFin == activePeriod.Value)
                    {
                        inPlay = false;
                        lastInicioForActive = null;
                    }
                }
            }

            if (!inPlay || lastInicioForActive == null)
                return;

            if (tracked.ClockAccumulatedSeconds > 0)
            {
                tracked.ClockPeriodAnchorUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            var kick = lastInicioForActive.OccurredAt;
            if (kick.HasValue)
            {
                var k = kick.Value;
                if (k.Kind == DateTimeKind.Unspecified)
                    k = DateTime.SpecifyKind(k, DateTimeKind.Utc);
                else if (k.Kind == DateTimeKind.Local)
                    k = k.ToUniversalTime();
                tracked.ClockPeriodAnchorUtc = k;
            }
            else
            {
                tracked.ClockPeriodAnchorUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<MatchReportResponse?> BuildMatchReportAsync(Guid id)
        {
            var match = await _context.Matches.AsNoTracking()
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .Include(m => m.Venue)
                .Include(m => m.Discipline)
                .Include(m => m.Phase)
                    .ThenInclude(p => p.Competition)
                        .ThenInclude(c => c.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null) return null;

            var lineups = await _context.MatchLineups.AsNoTracking()
                .Where(l => l.MatchId == id)
                .Include(l => l.Team)
                .Include(l => l.Players)
                    .ThenInclude(p => p.Player)
                .ToListAsync();

            var events = await _context.MatchEvents.AsNoTracking()
                .Where(e => e.MatchId == id)
                .Include(e => e.Player)
                .Include(e => e.RelatedPlayer)
                .ToListAsync();

            var teams = new List<MatchReportTeamResponse>();
            foreach (var teamId in new[] { match.LocalTeamId, match.VisitorTeamId }.Where(x => x.HasValue).Select(x => x!.Value))
            {
                var teamName = teamId == match.LocalTeamId
                    ? match.LocalTeam?.Name ?? "Local"
                    : match.VisitorTeam?.Name ?? "Visitante";

                var lineup = lineups.FirstOrDefault(l => l.TeamId == teamId);
                var rows = new Dictionary<Guid, MatchReportPlayerResponse>();

                if (lineup != null)
                {
                    foreach (var lp in lineup.Players)
                    {
                        rows[lp.PlayerId] = new MatchReportPlayerResponse
                        {
                            PlayerId = lp.PlayerId,
                            PlayerName = string.IsNullOrWhiteSpace(lp.Player?.Name)
                                ? "Jugador (sin datos)"
                                : lp.Player!.Name,
                            Number = lp.ShirtNumber ?? lp.Player?.Number,
                            Role = lp.Role == MatchLineupPlayerRole.Starter ? "Titular" : "Suplente",
                            Observation = lp.Observation
                        };
                    }
                }

                foreach (var ev in events.Where(e => e.TeamId == teamId))
                {
                    EnsureReportRow(rows, ev.Player, "No convocado");
                    EnsureReportRow(rows, ev.RelatedPlayer, "No convocado");

                    if (ev.PlayerId.HasValue && rows.TryGetValue(ev.PlayerId.Value, out var row))
                    {
                        // Penales de tanda no cuentan como goles de jugador en la planilla.
                        if (ev.Type == MatchEventType.Goal || ev.Type == MatchEventType.Puntaje)
                            row.Goals += Math.Max(ev.Value, 1);
                        else if (ev.Type == MatchEventType.TarjetaAmarilla)
                            row.YellowCards += 1;
                        else if (ev.Type == MatchEventType.SegundaAmarilla)
                            row.SecondYellowCards += 1;
                        else if (ev.Type == MatchEventType.TarjetaRoja)
                            row.DirectRedCards += 1;
                        else if (ev.Type == MatchEventType.RojaPorDobleAmarilla)
                            row.DoubleYellowRedCards += 1;
                        else if (ev.Type == MatchEventType.Sustitucion)
                            row.SubstitutionsOut += 1;
                    }

                    if (ev.Type == MatchEventType.Sustitucion
                        && ev.RelatedPlayerId.HasValue
                        && rows.TryGetValue(ev.RelatedPlayerId.Value, out var relatedRow))
                    {
                        relatedRow.SubstitutionsIn += 1;
                    }
                }

                var players = rows.Values
                    .OrderBy(p => p.Role == "Titular" ? 0 : p.Role == "Suplente" ? 1 : 2)
                    .ThenBy(p => p.Number ?? 999)
                    .ThenBy(p => p.PlayerName)
                    .ToList();

                teams.Add(new MatchReportTeamResponse
                {
                    TeamId = teamId,
                    TeamName = teamName,
                    StartersCount = players.Count(p => p.Role == "Titular"),
                    SubstitutesCount = players.Count(p => p.Role == "Suplente"),
                    Players = players
                });
            }

            var teamNameById = new Dictionary<Guid, string>();
            if (match.LocalTeamId.HasValue)
                teamNameById[match.LocalTeamId.Value] = match.LocalTeam?.Name ?? "Local";
            if (match.VisitorTeamId.HasValue)
                teamNameById[match.VisitorTeamId.Value] = match.VisitorTeam?.Name ?? "Visitante";

            var timeline = BuildMatchReportTimeline(events, teamNameById);
            var (leftLogoUrl, rightLogoUrl) = await ResolveReportLogoUrlsAsync(match.DisciplineId);

            return new MatchReportResponse
            {
                MatchId = match.Id,
                TournamentName = match.Phase?.Competition?.Tournament?.Name,
                CompetitionName = match.Phase?.Competition?.CategoryName,
                DisciplineName = match.Discipline?.Name,
                ScheduledAt = match.ScheduledAt,
                LocalScore = match.LocalScore,
                VisitorScore = match.VisitorScore,
                LocalPenaltyScore = match.LocalPenaltyScore ?? 0,
                VisitorPenaltyScore = match.VisitorPenaltyScore ?? 0,
                DecisionType = ResolveDecisionType(match, events),
                Teams = teams,
                LocalTeamName = match.LocalTeam?.Name,
                VisitorTeamName = match.VisitorTeam?.Name,
                VenueName = match.Venue?.Name,
                MatchNote = match.Note,
                StatusLabel = MatchStatusLabel(match.Status),
                Timeline = timeline,
                LeftLogoUrl = leftLogoUrl,
                RightLogoUrl = rightLogoUrl
            };
        }

        private async Task<(string? left, string? right)> ResolveReportLogoUrlsAsync(Guid disciplineId)
        {
            try
            {
                var rules = await _context.DisciplineRules.AsNoTracking()
                    .Where(r => r.DisciplineId == disciplineId
                        && (r.RuleKey == DisciplineActaLogoLeftRuleKey || r.RuleKey == DisciplineActaLogoRightRuleKey))
                    .ToListAsync();
                var left = rules.FirstOrDefault(r => r.RuleKey == DisciplineActaLogoLeftRuleKey)?.RuleValue;
                var right = rules.FirstOrDefault(r => r.RuleKey == DisciplineActaLogoRightRuleKey)?.RuleValue;

                string? defaultLeft = null;
                string? defaultRight = null;
                try
                {
                    defaultLeft = await _context.AppSettings.AsNoTracking()
                        .Where(s => s.Key == DefaultActaLogoLeftKey)
                        .Select(s => s.Value)
                        .FirstOrDefaultAsync();
                    defaultRight = await _context.AppSettings.AsNoTracking()
                        .Where(s => s.Key == DefaultActaLogoRightKey)
                        .Select(s => s.Value)
                        .FirstOrDefaultAsync();
                }
                catch
                {
                    // Sin tabla AppSettings migrada: solo logos por disciplina.
                }

                return (
                    !string.IsNullOrWhiteSpace(left) ? left : defaultLeft,
                    !string.IsNullOrWhiteSpace(right) ? right : defaultRight
                );
            }
            catch
            {
                return (null, null);
            }
        }

        private static string MatchStatusLabel(MatchStatus status) => status switch
        {
            MatchStatus.Programado => "Programado",
            MatchStatus.EnVivo => "En vivo",
            MatchStatus.Finalizado => "Finalizado",
            MatchStatus.Suspendido => "Suspendido",
            _ => status.ToString()
        };

        private static List<MatchReportEventLine> BuildMatchReportTimeline(
            List<MatchEvent> events,
            IReadOnlyDictionary<Guid, string> teamNames)
        {
            var list = new List<MatchReportEventLine>();
            foreach (var e in events.OrderBy(x => x.Period).ThenBy(x => x.Minute).ThenBy(x => x.Id))
            {
                var team = teamNames.TryGetValue(e.TeamId, out var tn) ? tn : null;
                switch (e.Type)
                {
                    case MatchEventType.Goal:
                    case MatchEventType.Puntaje:
                    {
                        var cat = e.Type == MatchEventType.Puntaje ? "Punto" : "Gol";
                        var who = e.Player?.Name ?? "—";
                        var num = e.Player?.Number;
                        var note = string.IsNullOrWhiteSpace(e.Note) ? "" : $" ({e.Note})";
                        var pts = e.Type == MatchEventType.Puntaje && e.Value > 1 ? $" (+{e.Value})" : "";
                        var body = num.HasValue ? $"{who} (#{num}){pts}" : $"{who}{pts}";
                        list.Add(new MatchReportEventLine
                        {
                            Minute = e.Minute,
                            Period = e.Period,
                            Category = cat,
                            TeamName = team,
                            Text = body + note
                        });
                        break;
                    }
                    case MatchEventType.PenaltyGoal:
                    {
                        var who = e.Player?.Name ?? "—";
                        var num = e.Player?.Number;
                        var note = string.IsNullOrWhiteSpace(e.Note) ? "" : $" ({e.Note})";
                        var body = num.HasValue ? $"{who} (#{num})" : who;
                        list.Add(new MatchReportEventLine
                        {
                            Minute = e.Minute,
                            Period = e.Period,
                            Category = "Penal convertido",
                            TeamName = team,
                            Text = body + note
                        });
                        break;
                    }
                    case MatchEventType.PenaltyMiss:
                    {
                        var who = e.Player?.Name ?? "—";
                        var num = e.Player?.Number;
                        var note = string.IsNullOrWhiteSpace(e.Note) ? "" : $" ({e.Note})";
                        var body = num.HasValue ? $"{who} (#{num})" : who;
                        list.Add(new MatchReportEventLine
                        {
                            Minute = e.Minute,
                            Period = e.Period,
                            Category = "Penal fallado",
                            TeamName = team,
                            Text = body + note
                        });
                        break;
                    }
                    case MatchEventType.TarjetaAmarilla:
                    case MatchEventType.SegundaAmarilla:
                    case MatchEventType.TarjetaRoja:
                    case MatchEventType.RojaPorDobleAmarilla:
                    {
                        var label = e.Type switch
                        {
                            MatchEventType.TarjetaAmarilla => "Amarilla",
                            MatchEventType.SegundaAmarilla => "2.ª amarilla",
                            MatchEventType.TarjetaRoja => "Roja",
                            MatchEventType.RojaPorDobleAmarilla => "Roja (doble amarilla)",
                            _ => "Tarjeta"
                        };
                        var who = e.Player?.Name ?? "—";
                        var num = e.Player?.Number;
                        var body = num.HasValue ? $"{who} (#{num})" : who;
                        list.Add(new MatchReportEventLine
                        {
                            Minute = e.Minute,
                            Period = e.Period,
                            Category = "Tarjeta",
                            TeamName = team,
                            Text = $"{label}: {body}"
                        });
                        break;
                    }
                    case MatchEventType.Sustitucion:
                    {
                        var outP = e.Player?.Name ?? "—";
                        var inP = e.RelatedPlayer?.Name ?? "—";
                        list.Add(new MatchReportEventLine
                        {
                            Minute = e.Minute,
                            Period = e.Period,
                            Category = "Cambio",
                            TeamName = team,
                            Text = $"Sale {outP} · Entra {inP}"
                        });
                        break;
                    }
                    case MatchEventType.InicioPeriodo:
                    case MatchEventType.FinPeriodo:
                        list.Add(new MatchReportEventLine
                        {
                            Minute = e.Minute,
                            Period = e.Period,
                            Category = "Periodo",
                            TeamName = null,
                            Text = e.Type == MatchEventType.InicioPeriodo
                                ? $"Inicio periodo {e.Period}"
                                : $"Fin periodo {e.Period}"
                        });
                        break;
                    default:
                        break;
                }
            }

            return list;
        }

        private static string ResolveDecisionType(Match match, List<MatchEvent> events)
        {
            var local = match.LocalScore;
            var visitor = match.VisitorScore;
            var localPen = match.LocalPenaltyScore ?? 0;
            var visitorPen = match.VisitorPenaltyScore ?? 0;
            var hadExtraTime = events.Any(e =>
                (e.Type == MatchEventType.InicioPeriodo || e.Type == MatchEventType.FinPeriodo)
                && e.Period > 2);

            if (localPen != visitorPen)
                return "Penales";
            if (local != visitor && hadExtraTime)
                return "Tiempo extra";
            if (local != visitor)
                return "Tiempo reglamentario";
            return "Empate";
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

        private async Task AutoLockRostersForMatchDetailAsync(Guid matchId)
        {
            var row = await _context.Matches.AsNoTracking()
                .Where(m => m.Id == matchId)
                .Select(m => new
                {
                    m.ScheduledAt,
                    CompetitionId = m.Phase.CompetitionId,
                    m.LocalTeamId,
                    m.VisitorTeamId
                })
                .FirstOrDefaultAsync();
            if (row == null || row.ScheduledAt.Year < 1900)
                return;

            var scheduled = DateTime.SpecifyKind(row.ScheduledAt, DateTimeKind.Local).ToUniversalTime();
            if (scheduled > DateTime.UtcNow.AddMinutes(5))
                return;

            var teamIds = new[] { row.LocalTeamId, row.VisitorTeamId }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
            if (teamIds.Count == 0)
                return;

            var rosterRows = await _context.CompetitionTeams
                .Where(ct => ct.CompetitionId == row.CompetitionId
                    && teamIds.Contains(ct.TeamId)
                    && !ct.RosterLocked)
                .ToListAsync();
            if (rosterRows.Count == 0)
                return;

            var now = DateTime.UtcNow;
            foreach (var roster in rosterRows)
            {
                roster.RosterLocked = true;
                roster.RosterLockedAt = now;
            }

            await _context.SaveChangesAsync();
        }

        private static void EnsureReportRow(Dictionary<Guid, MatchReportPlayerResponse> rows, Player? player, string role)
        {
            if (player == null || rows.ContainsKey(player.Id))
                return;

            rows[player.Id] = new MatchReportPlayerResponse
            {
                PlayerId = player.Id,
                PlayerName = string.IsNullOrWhiteSpace(player.Name)
                    ? "Jugador (sin datos)"
                    : player.Name,
                Number = player.Number,
                Role = role
            };
        }

        private static string Csv(string value)
        {
            var safe = value.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }

        /// <summary>Mismo criterio que <see cref="Hubs.TournamentHub.JoinMatchRoom"/>.</summary>
        private static string MatchRoomGroup(Guid matchId) => matchId.ToString().ToLower();

        private async Task<string?> ResolveLineupDelegateLabelAsync(Guid teamId)
        {
            var principal = await _context.TeamGestores.AsNoTracking()
                .Where(g => g.TeamId == teamId && g.Kind == TeamGestorKind.Principal)
                .Join(_context.Usuarios.AsNoTracking(), g => g.UsuarioId, u => u.Id, (g, u) => u)
                .Join(_context.Personas.AsNoTracking(), u => u.PersonaId, p => p.Id,
                    (u, p) => (p.Nombres + " " + p.Apellidos).Trim())
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(principal))
                return principal;

            var creator = await _context.Teams.AsNoTracking()
                .Where(t => t.Id == teamId && t.CreatedByUsuarioId != null)
                .Join(_context.Usuarios.AsNoTracking(), t => t.CreatedByUsuarioId!.Value, u => u.Id, (t, u) => u)
                .Join(_context.Personas.AsNoTracking(), u => u.PersonaId, p => p.Id,
                    (u, p) => (p.Nombres + " " + p.Apellidos).Trim())
                .FirstOrDefaultAsync();
            return creator;
        }

        private static object HubMatchSnapshot(Match m) => new
        {
            matchId = m.Id,
            status = m.Status.ToString(),
            localScore = m.LocalScore,
            visitorScore = m.VisitorScore,
            localPenaltyScore = m.LocalPenaltyScore ?? 0,
            visitorPenaltyScore = m.VisitorPenaltyScore ?? 0,
            clockAccumulatedSeconds = m.ClockAccumulatedSeconds,
            clockPeriodAnchorUtc = m.ClockPeriodAnchorUtc,
            clockWidgetKind = m.ClockWidgetKind.ToString(),
        };

        private async Task RepairEnVivoKickoffAsync(Guid matchId, MatchStatus status, Guid? localTeamId)
        {
            if (status != MatchStatus.EnVivo || !localTeamId.HasValue)
                return;

            var inicios = await _context.MatchEvents
                .Where(e => e.MatchId == matchId && e.Type == MatchEventType.InicioPeriodo)
                .OrderBy(e => e.Period)
                .ToListAsync();

            if (inicios.Count == 0)
            {
                _context.MatchEvents.Add(new MatchEvent
                {
                    MatchId = matchId,
                    Minute = 0,
                    Period = 1,
                    Type = MatchEventType.InicioPeriodo,
                    TeamId = localTeamId.Value,
                    OccurredAt = DateTime.UtcNow,
                    Value = 0
                });
                await _context.SaveChangesAsync();
                return;
            }

            var sinMarcaTiempo = inicios.FirstOrDefault(e => e.OccurredAt == null);
            if (sinMarcaTiempo != null)
            {
                sinMarcaTiempo.OccurredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }

    public sealed class MatchReportResponse
    {
        public Guid MatchId { get; set; }
        public string? TournamentName { get; set; }
        public string? CompetitionName { get; set; }
        /// <summary>Nombre de la disciplina deportiva (p. ej. Fútbol 11).</summary>
        public string? DisciplineName { get; set; }
        public DateTime ScheduledAt { get; set; }
        public int LocalScore { get; set; }
        public int VisitorScore { get; set; }
        public int LocalPenaltyScore { get; set; }
        public int VisitorPenaltyScore { get; set; }
        public string DecisionType { get; set; } = "No definida";
        public List<MatchReportTeamResponse> Teams { get; set; } = new();
        public string? LocalTeamName { get; set; }
        public string? VisitorTeamName { get; set; }
        public string? VenueName { get; set; }
        public string? MatchNote { get; set; }
        public string StatusLabel { get; set; } = "";
        public List<MatchReportEventLine> Timeline { get; set; } = new();
        public string? LeftLogoUrl { get; set; }
        public string? RightLogoUrl { get; set; }
    }

    public sealed class MatchReportEventLine
    {
        public int Minute { get; set; }
        public int Period { get; set; }
        public string Category { get; set; } = "";
        public string? TeamName { get; set; }
        public string Text { get; set; } = "";
    }

    public sealed class MatchReportTeamResponse
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int StartersCount { get; set; }
        public int SubstitutesCount { get; set; }
        public List<MatchReportPlayerResponse> Players { get; set; } = new();
        public int TotalGoals => Players.Sum(p => p.Goals);
        public int TotalYellowCards => Players.Sum(p => p.YellowCards);
        public int TotalSecondYellowCards => Players.Sum(p => p.SecondYellowCards);
        public int TotalDirectRedCards => Players.Sum(p => p.DirectRedCards);
        public int TotalDoubleYellowRedCards => Players.Sum(p => p.DoubleYellowRedCards);
        public int TotalSubstitutionsOut => Players.Sum(p => p.SubstitutionsOut);
        public int TotalSubstitutionsIn => Players.Sum(p => p.SubstitutionsIn);
    }

    public sealed class MatchReportPlayerResponse
    {
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? Number { get; set; }
        public string Role { get; set; } = string.Empty;
        public int Goals { get; set; }
        public int YellowCards { get; set; }
        public int SecondYellowCards { get; set; }
        public int DirectRedCards { get; set; }
        public int DoubleYellowRedCards { get; set; }
        public int SubstitutionsOut { get; set; }
        public int SubstitutionsIn { get; set; }
        public string? Observation { get; set; }
    }
}
