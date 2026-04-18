using Microsoft.AspNetCore.SignalR;

namespace Siged.Api.Hubs
{
    public class TournamentHub : Hub
    {
        // Este método permite a los clientes unirse a un "cuarto" específico por Partido
        // Así, los que ven el Partido A no reciben notificaciones del Partido B.
        public async Task JoinMatchRoom(string matchId)
        {
            // Forzamos ToLower() para que el nombre del grupo sea siempre consistente
            await Groups.AddToGroupAsync(Context.ConnectionId, matchId.ToLower());
        }
    }
}
