using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;
using System.Text.Json.Nodes;

namespace Siged.Api.Services;

/// <summary>
/// Alinea el JSON del widget con el acta: marcador, tarjetas y panel de estadísticas (fútbol) desde eventos.
/// </summary>
public static class MatchBroadcastWidgetActaSync
{
    private const string TemplateSoccer = "soccer";

    public static async Task<string> MergeAfterActaChangeAsync(
        ApplicationDbContext db,
        Guid matchId,
        Match match,
        string currentJson,
        CancellationToken ct = default)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(currentJson);
        }
        catch (JsonException)
        {
            return currentJson;
        }

        if (root is not JsonObject obj)
            return currentJson;

        if (obj["v"]?.GetValue<int>() is not 2)
            return currentJson;

        var sport = obj["sport"] as JsonObject ?? new JsonObject();
        obj["sport"] = sport;

        sport["scoreHome"] = match.LocalScore;
        sport["scoreAway"] = match.VisitorScore;

        var template = obj["template"]?.GetValue<string>() ?? "time";
        if (string.Equals(template, TemplateSoccer, StringComparison.Ordinal))
        {
            var agg = await AggregateSoccerFromEventsAsync(db, matchId, match.LocalTeamId, match.VisitorTeamId, ct);
            sport["yellowHome"] = agg.YellowHome;
            sport["yellowAway"] = agg.YellowAway;
            sport["redHome"] = agg.RedHome;
            sport["redAway"] = agg.RedAway;

            var stats = obj["statsPanel"] as JsonObject ?? new JsonObject();
            obj["statsPanel"] = stats;
            EnsureStatsPanelStructure(stats);
            if (StatsPanelSyncFromActa(stats))
            {
                // No pisar ajustes hechos solo desde mesa (±): conservar el mayor entre acta y valor actual.
                stats["shotsHome"] = Math.Max(agg.ShotsHome, ReadStatInt(stats, "shotsHome"));
                stats["shotsAway"] = Math.Max(agg.ShotsAway, ReadStatInt(stats, "shotsAway"));
                stats["shotsOnTargetHome"] = Math.Max(agg.ShotsOnTargetHome, ReadStatInt(stats, "shotsOnTargetHome"));
                stats["shotsOnTargetAway"] = Math.Max(agg.ShotsOnTargetAway, ReadStatInt(stats, "shotsOnTargetAway"));
                stats["foulsHome"] = Math.Max(agg.FoulsHome, ReadStatInt(stats, "foulsHome"));
                stats["foulsAway"] = Math.Max(agg.FoulsAway, ReadStatInt(stats, "foulsAway"));
                stats["offsidesHome"] = Math.Max(agg.OffsidesHome, ReadStatInt(stats, "offsidesHome"));
                stats["offsidesAway"] = Math.Max(agg.OffsidesAway, ReadStatInt(stats, "offsidesAway"));
                stats["yellowsHome"] = Math.Max(agg.YellowHome, ReadStatInt(stats, "yellowsHome"));
                stats["yellowsAway"] = Math.Max(agg.YellowAway, ReadStatInt(stats, "yellowsAway"));
                stats["redsHome"] = Math.Max(agg.RedHome, ReadStatInt(stats, "redsHome"));
                stats["redsAway"] = Math.Max(agg.RedAway, ReadStatInt(stats, "redsAway"));
            }
        }

        foreach (var key in new[]
                 {
                     "yellowHome", "yellowAway", "redHome", "redAway",
                     "cornersHome", "cornersAway", "freeKicksHome", "freeKicksAway"
                 })
        {
            if (!sport.ContainsKey(key))
                sport[key] = 0;
        }

        if (!obj.ContainsKey("showZonaHorariaOnMatch"))
            obj["showZonaHorariaOnMatch"] = false;

        obj["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return obj.ToJsonString();
    }

    private static int ReadStatInt(JsonObject stats, string key)
    {
        if (!stats.TryGetPropertyValue(key, out var node) || node is null) return 0;
        try
        {
            return Math.Max(0, node.GetValue<int>());
        }
        catch
        {
            return 0;
        }
    }

    private static void EnsureStatsPanelStructure(JsonObject stats)
    {
        if (!stats.ContainsKey("enabled")) stats["enabled"] = false;
        if (!stats.ContainsKey("showDuringPeriodBreak")) stats["showDuringPeriodBreak"] = false;
        if (!stats.ContainsKey("forceStatsOverlay")) stats["forceStatsOverlay"] = false;
        if (!stats.ContainsKey("syncStatsFromActa")) stats["syncStatsFromActa"] = true;
        foreach (var (k, defBool) in new[]
                 {
                     ("showShots", true), ("showShotsOnTarget", true), ("showFouls", true),
                     ("showOffsides", true), ("showYellows", true), ("showReds", true),
                     ("showPossession", true), ("showCorners", true)
                 })
        {
            if (!stats.ContainsKey(k))
                stats[k] = defBool;
        }

        if (!stats.ContainsKey("possessionHomePct"))
            stats["possessionHomePct"] = 50;
    }

    /// <summary>
    /// Si es false, la mesa controla los contadores del panel y no se pisan al guardar eventos del acta.
    /// </summary>
    private static bool StatsPanelSyncFromActa(JsonObject stats)
    {
        if (!stats.TryGetPropertyValue("syncStatsFromActa", out var n) || n is null)
            return true;
        return n.GetValueKind() != JsonValueKind.False;
    }

    private sealed record SoccerAgg(
        int ShotsHome, int ShotsAway,
        int ShotsOnTargetHome, int ShotsOnTargetAway,
        int FoulsHome, int FoulsAway,
        int OffsidesHome, int OffsidesAway,
        int YellowHome, int YellowAway,
        int RedHome, int RedAway);

    private static async Task<SoccerAgg> AggregateSoccerFromEventsAsync(
        ApplicationDbContext db,
        Guid matchId,
        Guid? localTeamId,
        Guid? visitorTeamId,
        CancellationToken ct)
    {
        var rows = await db.MatchEvents.AsNoTracking()
            .Where(e => e.MatchId == matchId)
            .Select(e => new { e.TeamId, e.Type })
            .ToListAsync(ct);

        var sh = 0;
        var sa = 0;
        var soth = 0;
        var sota = 0;
        var fh = 0;
        var fa = 0;
        var oh = 0;
        var oa = 0;
        var yh = 0;
        var ya = 0;
        var rh = 0;
        var ra = 0;

        foreach (var e in rows)
        {
            var isLocal = localTeamId.HasValue && e.TeamId == localTeamId;
            var isVisitor = visitorTeamId.HasValue && e.TeamId == visitorTeamId;
            if (!isLocal && !isVisitor) continue;

            switch (e.Type)
            {
                case MatchEventType.Tiro:
                    if (isLocal) sh++; else sa++;
                    break;
                case MatchEventType.TiroAPuerta:
                    if (isLocal) soth++; else sota++;
                    break;
                case MatchEventType.Falta:
                    if (isLocal) fh++; else fa++;
                    break;
                case MatchEventType.Offside:
                    if (isLocal) oh++; else oa++;
                    break;
                case MatchEventType.TarjetaAmarilla:
                    if (isLocal) yh++; else ya++;
                    break;
                case MatchEventType.TarjetaRoja:
                    if (isLocal) rh++; else ra++;
                    break;
            }
        }

        return new SoccerAgg(sh, sa, soth, sota, fh, fa, oh, oa, yh, ya, rh, ra);
    }
}
