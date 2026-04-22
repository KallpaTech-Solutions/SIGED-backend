using Microsoft.AspNetCore.SignalR;
using Siged.Api.Hubs;

namespace Siged.Api.Services;

/// <summary>
/// Notifica a la vitrina pública (/torneos) para refrescar listados sin recargar la página.
/// </summary>
public class TournamentVitrinaBroadcastService
{
    private readonly IHubContext<TournamentHub> _hubContext;

    public TournamentVitrinaBroadcastService(IHubContext<TournamentHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyLandingRefreshAsync() =>
        _hubContext.Clients.Group(TournamentHub.LandingFeedGroup).SendAsync("ReceiveLandingRefresh");

    public Task NotifyTournamentsRefreshAsync() =>
        _hubContext.Clients.Group(TournamentHub.TournamentsFeedGroup).SendAsync("ReceiveTournamentsRefresh");
}
