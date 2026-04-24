using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Hubs;
using Siged.Api.Services;
using Siged.Application.DTOs.Tournaments.Match;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchesController : ControllerBase
    {
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
                LocalTeamName = match.LocalTeam?.Name ?? "Por asignar",
                LocalTeamLogo = match.LocalTeam?.LogoUrl,
                VisitorTeamName = match.VisitorTeam?.Name ?? "Por asignar",
                VisitorTeamLogo = match.VisitorTeam?.LogoUrl,
                DisciplineName = match.Discipline?.Name ?? disciplineTitle,
                PhaseName = match.Phase.Name,
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
                Events = events
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
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound("El partido no existe.");
            if (match.Status == MatchStatus.Finalizado)
                return BadRequest("El partido ya fue finalizado previamente.");

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

                await transaction.CommitAsync();

                // 🚀 DETECCIÓN AUTOMÁTICA DE CAMPEÓN
                // Comprobamos si el nombre de la fase contiene "FINAL" (puedes usar el Order también)
                var phase = match.Phase;
                bool isGrandFinal = phase?.Name.Contains("FINAL", StringComparison.OrdinalIgnoreCase) == true;

                if (isGrandFinal && match.WinnerId.HasValue && phase != null)
                {
                    var champion = match.WinnerId == match.LocalTeamId ? match.LocalTeam : match.VisitorTeam;
                    if (champion != null)
                    {
                        // Emitimos un evento especial para TODO el Hub o la Competición
                        await _hubContext.Clients.All.SendAsync("ReceiveChampion", new
                        {
                            competitionId = phase.CompetitionId,
                            championName = champion.Name,
                            championLogo = champion.LogoUrl,
                            score = $"{match.LocalScore} - {match.VisitorScore}",
                            message = $"¡FELICIDADES {champion.Name}! CAMPEÓN DE LA {phase.Name.ToUpper()}"
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

                return Ok(new { message = "Partido finalizado y campeón detectado.", winnerId = match.WinnerId });
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

        /// <summary>Mismo criterio que <see cref="Hubs.TournamentHub.JoinMatchRoom"/>.</summary>
        private static string MatchRoomGroup(Guid matchId) => matchId.ToString().ToLower();

        private static object HubMatchSnapshot(Match m) => new
        {
            matchId = m.Id,
            status = m.Status.ToString(),
            localScore = m.LocalScore,
            visitorScore = m.VisitorScore,
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
}
