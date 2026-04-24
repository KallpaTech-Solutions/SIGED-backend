using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Api.Hubs;
using Siged.Api.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace Siged.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZonaHorariaController : ControllerBase
{
    private readonly ZonaHorariaPublicStateStore _store;
    private readonly IHubContext<TournamentHub> _hub;

    public ZonaHorariaController(ZonaHorariaPublicStateStore store, IHubContext<TournamentHub> hub)
    {
        _store = store;
        _hub = hub;
    }

    /// <summary>Estado actual para la franja pública y clientes anónimos (sin JWT).</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public ContentResult GetPublic()
    {
        var json = _store.GetSnapshotJson();
        return Content(json, "application/json");
    }

    /// <summary>Solo SUPERADMIN puede publicar el estado que ven todos los visitantes.</summary>
    [HttpPut("public")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> PutPublic([FromBody] JsonElement body)
    {
        if (!_store.TrySetSnapshot(body, out var err))
            return BadRequest(new { error = err });

        var snapshot = _store.GetSnapshotJson();
        await _hub.Clients.Group(TournamentHub.ZonaHorariaFeedGroup)
            .SendAsync("ReceiveZonaHorariaPublic", snapshot);

        return Ok();
    }
}
