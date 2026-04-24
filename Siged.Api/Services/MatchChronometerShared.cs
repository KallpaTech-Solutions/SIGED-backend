using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Api.Services
{
    /// <summary>Lógica compartida entre <see cref="Controllers.Core.Tournaments.MatchesController"/> y <see cref="Controllers.Core.Tournaments.MatchChronometerController"/>.</summary>
    public static class MatchChronometerShared
    {
        public static int NormInicioPeriod(MatchEvent ev)
        {
            var p = ev.Period;
            if (ev.Type == MatchEventType.InicioPeriodo && p <= 0)
                return 1;
            return p > 0 ? p : 1;
        }

        public static void FlushRunningClockSegment(Match match)
        {
            if (!match.ClockPeriodAnchorUtc.HasValue)
                return;

            var anchor = match.ClockPeriodAnchorUtc.Value;
            if (anchor.Kind == DateTimeKind.Unspecified)
                anchor = DateTime.SpecifyKind(anchor, DateTimeKind.Utc);
            else if (anchor.Kind == DateTimeKind.Local)
                anchor = anchor.ToUniversalTime();

            var delta = (int)Math.Max(0, (DateTime.UtcNow - anchor).TotalSeconds);
            match.ClockAccumulatedSeconds += delta;
            match.ClockPeriodAnchorUtc = null;
        }

        public static bool TryGetPeriodConfigFromRules(
            Dictionary<string, string> sportRules,
            out int periodsCount,
            out int periodDurationMin)
        {
            periodsCount = 0;
            periodDurationMin = 0;
            static string? RuleGet(Dictionary<string, string> m, params string[] keys)
            {
                foreach (var key in keys)
                {
                    foreach (var kv in m)
                    {
                        if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                            return kv.Value;
                    }
                }
                return null;
            }

            var pc = RuleGet(sportRules, "PERIODS_COUNT", "CANTIDAD_PERIODOS");
            var pd = RuleGet(sportRules, "PERIOD_DURATION");
            if (!int.TryParse(pc?.Trim(), out var pCount) || pCount <= 0) return false;
            if (!int.TryParse(pd?.Trim(), out var pDur) || pDur <= 0) return false;
            periodsCount = pCount;
            periodDurationMin = pDur;
            return true;
        }

        public static void ResolvePeriodPlayStateFromMarks(
            IReadOnlyList<MatchEvent> marks,
            out int? activePeriod,
            out bool inPlay)
        {
            activePeriod = null;
            inPlay = false;
            foreach (var ev in marks)
            {
                if (ev.Type == MatchEventType.InicioPeriodo)
                {
                    var p = NormInicioPeriod(ev);
                    activePeriod = p;
                    inPlay = true;
                }
                else if (ev.Type == MatchEventType.FinPeriodo)
                {
                    var pFin = ev.Period > 0 ? ev.Period : activePeriod ?? 1;
                    if (activePeriod.HasValue && pFin == activePeriod.Value)
                        inPlay = false;
                }
            }
        }

        public static int GetTotalElapsedSecondsForClock(Match match)
        {
            var acc = Math.Max(0, match.ClockAccumulatedSeconds);
            if (!match.ClockPeriodAnchorUtc.HasValue)
                return acc;
            var anchor = match.ClockPeriodAnchorUtc.Value;
            if (anchor.Kind == DateTimeKind.Unspecified)
                anchor = DateTime.SpecifyKind(anchor, DateTimeKind.Utc);
            else if (anchor.Kind == DateTimeKind.Local)
                anchor = anchor.ToUniversalTime();
            return acc + (int)Math.Max(0, (DateTime.UtcNow - anchor).TotalSeconds);
        }
    }
}
