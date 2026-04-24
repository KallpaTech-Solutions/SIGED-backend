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

namespace Siged.Api.Controllers.Core.Tournaments
{
    /// <summary>
    /// Cronómetro del partido (pausa, periodos, tipo de widget). Separado de <see cref="MatchesController"/> para admitir varios modos por transmisión.
    /// </summary>
    [ApiController]
    [Route("api/matches/{matchId:guid}/chronometer")]
    [Authorize]
    public class MatchChronometerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TournamentHub> _hubContext;
        private readonly TournamentVitrinaBroadcastService _vitrina;
        private readonly MatchSportRulesBuilder _sportRules;

        public MatchChronometerController(
            ApplicationDbContext context,
            IHubContext<TournamentHub> hubContext,
            TournamentVitrinaBroadcastService vitrina,
            MatchSportRulesBuilder sportRules)
        {
            _context = context;
            _hubContext = hubContext;
            _vitrina = vitrina;
            _sportRules = sportRules;
        }

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

        /// <summary>Pausar o reanudar el cronómetro (independiente del estado de transmisión).</summary>
        [HttpPatch("run")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> PatchRun(Guid matchId, [FromBody] PatchMatchClockDto dto)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            if (dto.Paused)
            {
                MatchChronometerShared.FlushRunningClockSegment(match);
            }
            else
            {
                if (match.Status != MatchStatus.EnVivo)
                {
                    return BadRequest(new
                    {
                        message = "Para reanudar el cronómetro el partido debe estar en transmisión en vivo (Iniciar transmisión primero)."
                    });
                }

                if (!match.ClockPeriodAnchorUtc.HasValue)
                    match.ClockPeriodAnchorUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(MatchRoomGroup(match.Id))
                .SendAsync("ReceiveMatchUpdate", HubMatchSnapshot(match));

            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(match);
        }

        /// <summary>Fin de periodo actual + inicio del siguiente (o solo inicio en descanso). Respeta PERIODS_COUNT.</summary>
        [HttpPost("advance-period")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> AdvancePeriod(Guid matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Phase)
                    .ThenInclude(p => p.Competition)
                        .ThenInclude(c => c.Discipline)
                            .ThenInclude(d => d.Rules)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return NotFound();

            if (match.Status != MatchStatus.EnVivo)
            {
                return BadRequest(new
                {
                    message = "Solo se puede cambiar de periodo con el partido en transmisión en vivo."
                });
            }

            if (match.LocalTeamId == null)
                return BadRequest(new { message = "El partido no tiene equipo local asignado." });

            var comp = match.Phase?.Competition;
            if (comp == null)
                return BadRequest(new { message = "Partido sin competencia." });

            var sportRulesMap = await _sportRules.BuildMapAsync(
                comp.Id,
                comp.Discipline?.Rules,
                comp.Discipline?.TemplateKey);

            if (!MatchChronometerShared.TryGetPeriodConfigFromRules(sportRulesMap, out var periodsCount, out var periodDurationMin))
            {
                return BadRequest(new
                {
                    message = "Definí PERIODS_COUNT y PERIOD_DURATION en las reglas o en la plantilla de la disciplina."
                });
            }

            var marks = await _context.MatchEvents
                .Where(e => e.MatchId == matchId &&
                            (e.Type == MatchEventType.InicioPeriodo || e.Type == MatchEventType.FinPeriodo))
                .OrderBy(e => e.OccurredAt.HasValue ? 0 : 1)
                .ThenBy(e => e.OccurredAt)
                .ThenBy(e => e.Id)
                .ToListAsync();

            MatchChronometerShared.ResolvePeriodPlayStateFromMarks(marks, out var activePeriod, out var inPlay);
            var teamId = match.LocalTeamId.Value;
            var finAt = DateTime.UtcNow;
            var inicioAt = finAt.AddMilliseconds(1);

            if (inPlay)
            {
                var ap = activePeriod ?? 1;
                if (ap >= periodsCount)
                {
                    return BadRequest(new
                    {
                        message = "Ya estás en el último periodo. Finalizá el partido o registrá el fin de periodo a mano."
                    });
                }

                var elapsed = MatchChronometerShared.GetTotalElapsedSecondsForClock(match);
                var minute = Math.Min(periodDurationMin, Math.Max(0, (elapsed + 59) / 60));

                _context.MatchEvents.Add(new MatchEvent
                {
                    MatchId = matchId,
                    Minute = minute,
                    Type = MatchEventType.FinPeriodo,
                    TeamId = teamId,
                    Period = ap,
                    OccurredAt = finAt,
                    Value = 0
                });

                match.ClockAccumulatedSeconds = 0;
                match.ClockPeriodAnchorUtc = null;

                var nextP = ap + 1;
                _context.MatchEvents.Add(new MatchEvent
                {
                    MatchId = matchId,
                    Minute = 0,
                    Type = MatchEventType.InicioPeriodo,
                    TeamId = teamId,
                    Period = nextP,
                    OccurredAt = inicioAt,
                    Value = 0
                });

                match.ClockAccumulatedSeconds = 0;
                match.ClockPeriodAnchorUtc = DateTime.UtcNow;
            }
            else
            {
                if (!activePeriod.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "No hay periodo cerrado ni en juego. Iniciá el partido o registrá «Inicio de periodo»."
                    });
                }

                var nextP = activePeriod.Value + 1;
                if (nextP > periodsCount)
                {
                    return BadRequest(new
                    {
                        message = "No hay más periodos según PERIODS_COUNT."
                    });
                }

                _context.MatchEvents.Add(new MatchEvent
                {
                    MatchId = matchId,
                    Minute = 0,
                    Type = MatchEventType.InicioPeriodo,
                    TeamId = teamId,
                    Period = nextP,
                    OccurredAt = finAt,
                    Value = 0
                });

                match.ClockAccumulatedSeconds = 0;
                match.ClockPeriodAnchorUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(MatchRoomGroup(matchId))
                .SendAsync("ReceiveMatchUpdate", HubMatchSnapshot(match));

            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(match);
        }

        /// <summary>Tipo de widget de cronómetro para esta transmisión (<see cref="MatchClockWidgetKind.None"/> oculta el reloj).</summary>
        [HttpPatch("widget")]
        [Authorize(Policy = Permissions.TournMatchControl)]
        public async Task<IActionResult> PatchWidget(Guid matchId, [FromBody] PatchMatchClockWidgetDto dto)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound();

            match.ClockWidgetKind = dto.Kind;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(MatchRoomGroup(matchId))
                .SendAsync("ReceiveMatchUpdate", HubMatchSnapshot(match));

            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(match);
        }
    }
}
