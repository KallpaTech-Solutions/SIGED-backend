using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Constants;
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

        public MatchesController(
            ApplicationDbContext context,
            IHubContext<TournamentHub> hubContext,
            StandingsService standingsService,
            TournamentVitrinaBroadcastService vitrina)
        {
            _context = context;
            _hubContext = hubContext;
            _standingsService = standingsService;
            _vitrina = vitrina;
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
        /// </summary>
        [HttpGet("public/{id:guid}/detail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicMatchDetail(Guid id)
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
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (match == null) return NotFound();

            if (match.LocalTeam == null || match.VisitorTeam == null)
                return NotFound("Partido sin equipos asignados.");

            if (match.Phase?.Competition == null)
                return NotFound("Partido sin competencia asociada.");

            var comp = match.Phase.Competition;
            var tournament = comp.Tournament;
            if (tournament == null)
                return NotFound("Torneo no disponible para este partido.");

            var sportRules = new Dictionary<string, string>();
            if (comp.Discipline?.Rules != null)
            {
                foreach (var r in comp.Discipline.Rules)
                {
                    if (string.IsNullOrWhiteSpace(r.RuleKey)) continue;
                    sportRules[r.RuleKey] = r.RuleValue ?? "";
                }
            }

            var competitionRuleRows = await _context.CompetitionRules
                .AsNoTracking()
                .Where(r => r.CompetitionId == comp.Id)
                .ToListAsync();
            foreach (var r in competitionRuleRows)
            {
                if (string.IsNullOrWhiteSpace(r.RuleKey)) continue;
                sportRules[r.RuleKey] = r.RuleValue ?? "";
            }

            // Completar reglas típicas (PERIOD_DURATION, etc.) desde la plantilla oficial si faltan en BD.
            var tkRules = comp.Discipline?.TemplateKey;
            if (!string.IsNullOrWhiteSpace(tkRules))
            {
                foreach (var kv in SportRulesTemplates.OfficialTemplates)
                {
                    if (!string.Equals(kv.Key, tkRules, StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (var rule in kv.Value.Rules)
                    {
                        if (!sportRules.ContainsKey(rule.Key))
                            sportRules[rule.Key] = rule.Value;
                    }
                    break;
                }
            }

            await RepairEnVivoKickoffAsync(id, match.Status, match.LocalTeamId);

            var events = await _context.MatchEvents
                .AsNoTracking()
                .Include(e => e.Player)
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
                    TeamName = _context.Teams.Where(t => t.Id == e.TeamId).Select(t => t.Name).FirstOrDefault() ?? "Equipo",
                    PlayerName = e.Player != null ? e.Player.Name : null,
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
                LocalTeamName = match.LocalTeam.Name,
                LocalTeamLogo = match.LocalTeam.LogoUrl,
                VisitorTeamName = match.VisitorTeam.Name,
                VisitorTeamLogo = match.VisitorTeam.LogoUrl,
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
                LocalTeamName = match.LocalTeam.Name,
                LocalTeamLogo = match.LocalTeam.LogoUrl,
                LocalScore = match.LocalScore,
                VisitorTeamName = match.VisitorTeam.Name,
                VisitorTeamLogo = match.VisitorTeam.LogoUrl,
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
                .Include(e => e.Player)
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
                    TeamName = _context.Teams.Where(t => t.Id == e.TeamId).Select(t => t.Name).FirstOrDefault() ?? "Equipo",
                    PlayerName = e.Player != null ? e.Player.Name : null,
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

            match.Status = dto.Status;

            // Primer arranque en vivo: registrar inicio del 1.º periodo para el cronómetro (TeamId = local).
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
            }

            await _context.SaveChangesAsync();

            if (dto.Status == MatchStatus.EnVivo)
            {
                await _hubContext.Clients.Group(match.Id.ToString().ToLowerInvariant())
                    .SendAsync("ReceiveMatchUpdate", new { matchId = match.Id });
            }

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

            // 2. Creamos el evento
            var newEvent = new MatchEvent
            {
                MatchId = id,
                Minute = dto.Minute,
                Type = dto.Type,
                TeamId = dto.TeamId,
                PlayerId = dto.PlayerId,
                Note = dto.Note,
                Value = dto.Value,
                Period = dto.Period,
                OccurredAt = DateTime.UtcNow
            };

            _context.MatchEvents.Add(newEvent);

            // 💾 GUARDADO CRÍTICO: Aquí se guarda el evento Y el nuevo score del partido
            await _context.SaveChangesAsync();

            // 🚀 3. SignalR: Enviamos al grupo en minúsculas
            await _hubContext.Clients.Group(id.ToString().ToLower()).SendAsync("ReceiveMatchUpdate", new
            {
                matchId = id,
                localScore = match.LocalScore,
                visitorScore = match.VisitorScore,
                lastEvent = new
                {
                    minute = dto.Minute,
                    type = dto.Type.ToString(),
                    teamId = dto.TeamId,
                    note = dto.Note
                }
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

            return Ok(new { message = "Jugador actualizado", playerName });
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

            // 🚀 4. SignalR: Notificar a todos que el marcador cambió (hacia abajo)
            await _hubContext.Clients.Group(match.Id.ToString().ToLower()).SendAsync("ReceiveMatchUpdate", new
            {
                matchId = match.Id,
                localScore = match.LocalScore,
                visitorScore = match.VisitorScore,
                lastEvent = new
                {
                    type = "EVENT_DELETED",
                    message = "Un evento fue anulado. El marcador se ha actualizado.",
                    deletedEventId = eventId
                }
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
                bool isGrandFinal = match.Phase.Name.Contains("FINAL", StringComparison.OrdinalIgnoreCase);

                if (isGrandFinal && match.WinnerId.HasValue)
                {
                    var champion = match.WinnerId == match.LocalTeamId ? match.LocalTeam : match.VisitorTeam;

                    // Emitimos un evento especial para TODO el Hub o la Competición
                    await _hubContext.Clients.All.SendAsync("ReceiveChampion", new
                    {
                        competitionId = match.Phase.CompetitionId,
                        championName = champion.Name,
                        championLogo = champion.LogoUrl,
                        score = $"{match.LocalScore} - {match.VisitorScore}",
                        message = $"¡FELICIDADES {champion.Name}! CAMPEÓN DE LA {match.Phase.Name.ToUpper()}"
                    });
                }

                // SignalR estándar de fin de partido
                await _hubContext.Clients.Group(id.ToString().ToLower()).SendAsync("ReceiveMatchUpdate", new
                {
                    matchId = id,
                    status = "Finalizado",
                    winnerId = match.WinnerId,
                    finalScore = $"{match.LocalScore} - {match.VisitorScore}"
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
        /// Partido en vivo sin marca de tiempo en el inicio de periodo: el cronómetro queda en 0:00.
        /// Crea el 1T si falta o asigna <see cref="MatchEvent.OccurredAt"/> al primer inicio que lo necesite.
        /// </summary>
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
