using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Siged.Api.Hubs;
using Siged.Api.Services;
using Siged.Domain.Entities.Security;
using System.Text.Json;

namespace Siged.Api.Controllers;

/// <summary>
/// Widget de transmisión (tiempos visibles en la vitrina del partido). Solo quien controla la mesa puede editar.
/// </summary>
[ApiController]
[Route("api/Matches/{matchId:guid}/broadcast-widget")]
public class MatchBroadcastWidgetController : ControllerBase
{
    private readonly MatchBroadcastWidgetStore _store;
    private readonly IHubContext<TournamentHub> _hub;

    public MatchBroadcastWidgetController(MatchBroadcastWidgetStore store, IHubContext<TournamentHub> hub)
    {
        _store = store;
        _hub = hub;
    }

    private static string MatchRoomGroup(Guid id) => id.ToString().ToLowerInvariant();

    [HttpGet]
    [AllowAnonymous]
    public ContentResult Get(Guid matchId)
    {
        var json = _store.GetSnapshotJson(matchId);
        return Content(json, "application/json");
    }

    [HttpPut]
    [Authorize(Policy = Permissions.TournMesaBroadcast)]
    public async Task<IActionResult> Put(Guid matchId, [FromBody] JsonElement body)
    {
        if (!_store.TrySetSnapshot(matchId, body, out var err))
            return BadRequest(new { error = err });

        var snapshot = _store.GetSnapshotJson(matchId);
        await _hub.Clients.Group(MatchRoomGroup(matchId)).SendAsync("ReceiveMatchUpdate", new
        {
            matchId,
            broadcastWidgetJson = snapshot,
        });

        return Ok();
    }
}
