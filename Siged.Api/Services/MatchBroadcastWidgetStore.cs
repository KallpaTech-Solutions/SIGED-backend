using System.Collections.Concurrent;
using System.Text.Json;

namespace Siged.Api.Services;

/// <summary>
/// Widget de tiempo de transmisión por partido (cronómetro, reloj sistema, cuenta regresiva).
/// Misma forma lógica que ZonaHoraria pero con scope por MatchId.
/// </summary>
public sealed class MatchBroadcastWidgetStore
{
    private readonly ConcurrentDictionary<Guid, string> _jsonByMatch = new();

    public const string DefaultSnapshotJson =
        """{"v":2,"template":"time","updatedAt":0,"heroShowChrono":true,"heroShowSystemClock":false,"heroShowCountdown":false,"publicShowMs":true,"eventPeriod":1,"showZonaHorariaOnMatch":false,"statsPanel":{"enabled":false,"showDuringPeriodBreak":false,"showShots":true,"showShotsOnTarget":true,"showFouls":true,"showOffsides":true,"showYellows":true,"showReds":true,"showPossession":true,"showCorners":true,"possessionHomePct":50,"shotsHome":0,"shotsAway":0,"shotsOnTargetHome":0,"shotsOnTargetAway":0,"foulsHome":0,"foulsAway":0,"offsidesHome":0,"offsidesAway":0,"yellowsHome":0,"yellowsAway":0,"redsHome":0,"redsAway":0},"chrono":{"running":false,"baseMs":0,"startedAt":null},"countdown":{"configuredSec":60,"running":false,"endAt":null,"remainingMsFrozen":60000},"sport":{"labelHome":"","labelAway":"","scoreHome":0,"scoreAway":0,"period":1,"foulsHome":0,"foulsAway":0,"shotSec":24,"setsHome":0,"setsAway":0,"serveHome":true,"yellowHome":0,"yellowAway":0,"redHome":0,"redAway":0,"cornersHome":0,"cornersAway":0,"freeKicksHome":0,"freeKicksAway":0}}""";

    public string GetSnapshotJson(Guid matchId)
    {
        return _jsonByMatch.TryGetValue(matchId, out var s) ? s : DefaultSnapshotJson;
    }

    public bool TrySetSnapshot(Guid matchId, JsonElement body, out string? error)
    {
        error = null;
        if (body.ValueKind != JsonValueKind.Object)
        {
            error = "invalid_body";
            return false;
        }

        if (!body.TryGetProperty("v", out var vEl) || vEl.ValueKind != JsonValueKind.Number)
        {
            error = "invalid_v";
            return false;
        }

        var vNum = vEl.GetInt32();
        if (vNum != 1 && vNum != 2)
        {
            error = "invalid_v";
            return false;
        }

        _jsonByMatch[matchId] = body.GetRawText();
        return true;
    }
}
