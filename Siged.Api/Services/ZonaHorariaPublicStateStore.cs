using System.Text.Json;

namespace Siged.Api.Services;

/// <summary>
/// Estado del widget ZonaHoraria (demo) compartido entre SUPERADMIN y visitantes sin depender de localStorage del navegador.
/// </summary>
public sealed class ZonaHorariaPublicStateStore
{
    private readonly object _lock = new();
    private string _json;

    public ZonaHorariaPublicStateStore()
    {
        _json = DefaultSnapshotJson;
    }

    public const string DefaultSnapshotJson =
        """{"v":1,"updatedAt":0,"publicBannerEnabled":false,"publicShowMs":true,"publicShowMode":"both","chrono":{"running":false,"baseMs":0,"startedAt":null},"countdown":{"configuredSec":60,"running":false,"endAt":null,"remainingMsFrozen":60000}}""";

    public string GetSnapshotJson()
    {
        lock (_lock)
        {
            return _json;
        }
    }

    /// <summary>
    /// Valida v=1 y guarda el JSON crudo para devolverlo igual en GET.
    /// </summary>
    public bool TrySetSnapshot(JsonElement body, out string? error)
    {
        error = null;
        if (body.ValueKind != JsonValueKind.Object)
        {
            error = "invalid_body";
            return false;
        }

        if (!body.TryGetProperty("v", out var vEl) || vEl.ValueKind != JsonValueKind.Number || vEl.GetInt32() != 1)
        {
            error = "invalid_v";
            return false;
        }

        var raw = body.GetRawText();
        lock (_lock)
        {
            _json = raw;
        }

        return true;
    }
}
