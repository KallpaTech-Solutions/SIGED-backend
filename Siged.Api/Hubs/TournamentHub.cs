using Microsoft.AspNetCore.SignalR;

namespace Siged.Api.Hubs
{
    public class TournamentHub : Hub
    {
        public const string LandingFeedGroup = "landing-feed";
        public const string TournamentsFeedGroup = "tournaments-feed";
        /// <summary>Widget ZonaHoraria (demo): estado público sincronizado desde el API.</summary>
        public const string ZonaHorariaFeedGroup = "zona-horaria-feed";

        /// <summary>Vitrina: GET /api/Matches/public/landing cuando cambia el conjunto de partidos visibles.</summary>
        public async Task JoinLandingFeed() =>
            await Groups.AddToGroupAsync(Context.ConnectionId, LandingFeedGroup);

        /// <summary>Vitrina: GET /api/Tournaments cuando cambia el listado público.</summary>
        public async Task JoinTournamentsFeed() =>
            await Groups.AddToGroupAsync(Context.ConnectionId, TournamentsFeedGroup);

        // Este método permite a los clientes unirse a un "cuarto" específico por Partido
        // Así, los que ven el Partido A no reciben notificaciones del Partido B.
        public async Task JoinMatchRoom(string matchId)
        {
            // Forzamos ToLower() para que el nombre del grupo sea siempre consistente
            await Groups.AddToGroupAsync(Context.ConnectionId, matchId.ToLower());
        }

        public Task JoinZonaHorariaFeed() =>
            Groups.AddToGroupAsync(Context.ConnectionId, ZonaHorariaFeedGroup);
    }
}
