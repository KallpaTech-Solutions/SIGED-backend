using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Hubs;
using Siged.Application.DTOs.Tournaments.Match;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TournamentHub> _hubContext;
        private readonly StandingsService _standingsService;
        // Inyectamos el Hub en el constructor
        public MatchesController(ApplicationDbContext context, IHubContext<TournamentHub> hubContext, StandingsService standingsService)
        {
            _context = context;
            _hubContext = hubContext;
            _standingsService = standingsService;
        }
        // 1. Ver partidos de una fecha específica (Lo que verá la "Mesa")
        [HttpGet("journal/{journalId}")]
        public async Task<IActionResult> GetByJournal(Guid journalId)
        {
            var matches = await _context.Matches
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .Include(m => m.Venue)
                .Where(m => m.JournalId == journalId)
                .OrderBy(m => m.ScheduledAt)
                .ToListAsync();

            return Ok(matches);
        }

        [HttpGet("{id}/result")]
        public async Task<IActionResult> GetMatchResult(Guid id)
        {
            var match = await _context.Matches
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            var result = new MatchDetailDto
            {
                Id = match.Id,
                LocalTeamName = match.LocalTeam.Name,
                LocalTeamLogo = match.LocalTeam.LogoUrl,
                LocalScore = match.LocalScore,
                VisitorTeamName = match.VisitorTeam.Name,
                VisitorTeamLogo = match.VisitorTeam.LogoUrl,
                VisitorScore = match.VisitorScore,
                Status = match.Status.ToString(),
                ScheduledAt = match.ScheduledAt
            };

            return Ok(result);
        }
        [HttpGet("{id}/timeline")]
        public async Task<IActionResult> GetMatchTimeline(Guid id)
        {
            var events = await _context.MatchEvents
                .Include(e => e.Player)
                .Where(e => e.MatchId == id)
                .OrderByDescending(e => e.Minute) // Los más recientes primero
                .Select(e => new MatchEventDetailDto
                {
                    Id = e.Id,
                    Minute = e.Minute,
                    Type = e.Type.ToString(),
                    TeamName = _context.Teams.Where(t => t.Id == e.TeamId).Select(t => t.Name).FirstOrDefault() ?? "Equipo",
                    PlayerName = e.Player != null ? e.Player.Name : null,
                    Note = e.Note,
                    Value = e.Value,
                    Period = e.Period
                })
                .ToListAsync();

            return Ok(events);
        }
        // 3. Cambiar estado (Por si la mesa quiere ponerlo "En Juego" o "Postergado")
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MatchStatus status)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            match.Status = status;
            await _context.SaveChangesAsync();
            return Ok(match);
        }

        // 4. Asignar sede y hora (Programación)
        [HttpPatch("{id}/schedule")]
        public async Task<IActionResult> ScheduleMatch(Guid id, [FromBody] DateTime scheduledAt, [FromQuery] Guid venueId)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            match.ScheduledAt = scheduledAt;
            match.VenueId = venueId;

            await _context.SaveChangesAsync();
            return Ok(match);
        }

        [HttpPost("{id}/events")]
        public async Task<IActionResult> AddEvent(Guid id, [FromBody] MatchEventDto dto)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound();

            var newEvent = new MatchEvent
            {
                MatchId = id,
                Minute = dto.Minute,
                Type = dto.Type,
                TeamId = dto.TeamId,
                PlayerId = dto.PlayerId,
                Note = dto.Note,
                Value = dto.Value,
                Period = dto.Period
            };

            if (dto.Type == MatchEventType.Goal || dto.Type == MatchEventType.Puntaje)
            {
                if (match.LocalTeamId == dto.TeamId) match.LocalScore += dto.Value;
                else match.VisitorScore += dto.Value;
            }

            _context.MatchEvents.Add(newEvent);
            await _context.SaveChangesAsync();

            // 🚀 SignalR (Esto sigue igual, está perfecto)
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMatchUpdate", new
            {
                matchId = id,
                localScore = match.LocalScore,
                visitorScore = match.VisitorScore,
                lastEvent = new
                {
                    minute = dto.Minute,
                    type = dto.Type.ToString(),
                    teamId = dto.TeamId
                }
            });

            // ✅ DEVOLVEMOS EL DTO EN LUGAR DE LA ENTIDAD
            return Ok(new MatchEventResponseDto
            {
                Id = newEvent.Id,
                MatchId = newEvent.MatchId,
                Minute = newEvent.Minute,
                Type = newEvent.Type.ToString(),
                TeamId = newEvent.TeamId,
                PlayerId = newEvent.PlayerId,
                Note = newEvent.Note,
                Value = newEvent.Value,
                Period = newEvent.Period
            });
        }
        [HttpPatch("events/{eventId}/player")]
        public async Task<IActionResult> UpdateEventPlayer(Guid eventId, [FromBody] Guid? playerId)
        {
            // 1. Buscamos el evento
            var matchEvent = await _context.MatchEvents
                .Include(m => m.Match)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (matchEvent == null) return NotFound("Evento no encontrado.");

            // 2. Actualizamos solo el jugador
            matchEvent.PlayerId = playerId;
            await _context.SaveChangesAsync();

            // 3. ¡IMPORTANTE! Obtenemos el nombre del jugador para SignalR
            var playerName = "Jugador no informado";
            if (playerId.HasValue)
            {
                playerName = await _context.Players
                    .Where(p => p.Id == playerId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync() ?? "Jugador desconocido";
            }

            // 🚀 4. Avisamos por SignalR que el gol ya tiene nombre
            await _hubContext.Clients.Group(matchEvent.MatchId.ToString()).SendAsync("ReceiveEventUpdate", new
            {
                eventId = eventId,
                matchId = matchEvent.MatchId,
                playerId = playerId,
                playerName = playerName,
                message = $"¡Autor confirmado! Gol de {playerName}"
            });

            return Ok(new { message = "Jugador actualizado", playerName });
        }
        [HttpDelete("events/{eventId}")]
        public async Task<IActionResult> DeleteEvent(Guid eventId)
        {
            // 1. Buscar el evento incluyendo el partido relacionado
            var matchEvent = await _context.MatchEvents
                .Include(e => e.Match)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (matchEvent == null) return NotFound("El evento no existe.");

            var match = matchEvent.Match;

            // 2. Si el evento era un Gol o Puntaje, debemos REVERTIR el marcador
            if (matchEvent.Type == MatchEventType.Goal || matchEvent.Type == MatchEventType.Puntaje)
            {
                if (match.LocalTeamId == matchEvent.TeamId)
                    match.LocalScore -= matchEvent.Value;
                else
                    match.VisitorScore -= matchEvent.Value;

                // Aseguramos que el marcador no sea negativo (por si acaso)
                if (match.LocalScore < 0) match.LocalScore = 0;
                if (match.VisitorScore < 0) match.VisitorScore = 0;
            }

            // 3. Eliminar el evento de la base de datos
            _context.MatchEvents.Remove(matchEvent);
            await _context.SaveChangesAsync();

            // 🚀 4. SignalR: Notificar a todos que el marcador cambió (hacia abajo)
            await _hubContext.Clients.Group(match.Id.ToString()).SendAsync("ReceiveMatchUpdate", new
            {
                matchId = match.Id,
                localScore = match.LocalScore,
                visitorScore = match.VisitorScore,
                lastEvent = new
                {
                    type = "EVENT_DELETED",
                    message = "Un evento fue anulado. El marcador se ha actualizado.",
                    deletedEventId = eventId
                }
            });

            return Ok(new { message = "Evento eliminado y marcador corregido", localScore = match.LocalScore, visitorScore = match.VisitorScore });
        }

        [HttpPatch("{id}/finish")]
        public async Task<IActionResult> FinishMatch(Guid id)
        {
            // Buscamos el partido con sus datos relacionados
            var match = await _context.Matches
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound("El partido no existe.");

            // 1. Verificación de Seguridad (Tipado Nullable)
            if (!match.GroupId.HasValue)
            {
                return BadRequest("Este partido no tiene un grupo asignado. No se puede actualizar la tabla.");
            }

            // 2. Validación de Estado (Evitar duplicidad)
            if (match.Status == MatchStatus.Finalizado)
            {
                return BadRequest("El partido ya fue finalizado previamente.");
            }

            // 3. Persistencia
            match.Status = MatchStatus.Finalizado;
            await _context.SaveChangesAsync();

            // 4. Cálculo de la Tabla (usando .Value para convertir Guid? a Guid)
            var groupId = match.GroupId.Value;
            var updatedStandings = await _standingsService.GetStandingsByGroupAsync(groupId);

            // 🚀 5. SignalR: Notificar Cierre de Partido
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMatchUpdate", new
            {
                matchId = id,
                status = "Finalizado",
                message = "🏁 ¡Pitazo final! El marcador ha sido sellado."
            });

            // 🚀 6. SignalR: Notificar Nueva Tabla de Posiciones
            await _hubContext.Clients.Group(groupId.ToString()).SendAsync("ReceiveStandingsUpdate", new
            {
                groupId = groupId,
                standings = updatedStandings
            });

            return Ok(new
            {
                message = "Partido finalizado y tabla actualizada.",
                finalScore = $"{match.LocalScore} - {match.VisitorScore}"
            });
        }
    }
}
