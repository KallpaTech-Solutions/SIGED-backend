using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Hubs;
using Siged.Application.DTOs.Tournaments.Match;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        /// <summary>
        /// Registrar un evento en el partido (gol, tarjeta, sustitución, etc.) y actualizar el marcador si es necesario.
        /// ⚽ Este endpoint es crucial para el seguimiento en tiempo real del partido. Asegúrate de enviar el evento 
        /// correcto con el TeamId correspondiente para que el marcador se actualice automáticamente.
        /// </summary>
        /// <param name="id">ID del partido</param>
        /// <param name="dto">Datos del evento</param>
        /// <returns>Resultado de la operación</returns>
        [HttpPost("{id}/events")]
        public async Task<IActionResult> AddEvent(Guid id, [FromBody] MatchEventDto dto)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null) return NotFound("Partido no encontrado");

            // ⚽ 1. Lógica de Marcador ÚNICA y VALIDADA
            if (dto.Type == MatchEventType.Goal || dto.Type == MatchEventType.Puntaje)
            {
                if (match.LocalTeamId == dto.TeamId)
                    match.LocalScore += dto.Value;
                else if (match.VisitorTeamId == dto.TeamId)
                    match.VisitorScore += dto.Value;
                else
                    return BadRequest("El TeamId enviado no pertenece a este partido.");
            }
            else if (dto.Type == MatchEventType.PenaltyGoal)
            {
                if (match.LocalTeamId == dto.TeamId) match.LocalPenaltyScore += 1;
                else if (match.VisitorTeamId == dto.TeamId) match.VisitorPenaltyScore += 1;
            }

            // 2. Creamos el evento
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

            _context.MatchEvents.Add(newEvent);

            // 💾 GUARDADO CRÍTICO: Aquí se guarda el evento Y el nuevo score del partido
            await _context.SaveChangesAsync();

            // 🚀 3. SignalR: Enviamos al grupo en minúsculas
            await _hubContext.Clients.Group(id.ToString().ToLower()).SendAsync("ReceiveMatchUpdate", new
            {
                matchId = id,
                localScore = match.LocalScore,
                visitorScore = match.VisitorScore,
                lastEvent = new
                {
                    minute = dto.Minute,
                    type = dto.Type.ToString(),
                    teamId = dto.TeamId,
                    note = dto.Note
                }
            });

            return Ok(new { message = "Evento y marcador actualizados", score = $"{match.LocalScore}-{match.VisitorScore}" });
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
            // 1. Incluimos Phase para saber si es la Final y los Teams para el nombre/logo
            var match = await _context.Matches
                .Include(m => m.Phase)
                .Include(m => m.LocalTeam)
                .Include(m => m.VisitorTeam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound("El partido no existe.");
            if (match.Status == MatchStatus.Finalizado)
                return BadRequest("El partido ya fue finalizado previamente.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🏆 LÓGICA DE GANADOR (Mantenemos tu lógica actual)
                if (match.LocalScore > match.VisitorScore)
                    match.WinnerId = match.LocalTeamId;
                else if (match.VisitorScore > match.LocalScore)
                    match.WinnerId = match.VisitorTeamId;
                else
                {
                    if (match.LocalPenaltyScore > match.VisitorPenaltyScore)
                        match.WinnerId = match.LocalTeamId;
                    else if (match.VisitorPenaltyScore > match.LocalPenaltyScore)
                        match.WinnerId = match.VisitorTeamId;
                }

                match.Status = MatchStatus.Finalizado;
                await _context.SaveChangesAsync();

                // PERSISTENCIA DE ESTADÍSTICAS... (tu código de Standings se mantiene igual)
                if (match.GroupId.HasValue)
                {
                    await _standingsService.UpdateGroupStandingsAsync(match.GroupId.Value);
                    // ... (notificación de standings)
                }

                await transaction.CommitAsync();

                // 🚀 DETECCIÓN AUTOMÁTICA DE CAMPEÓN
                // Comprobamos si el nombre de la fase contiene "FINAL" (puedes usar el Order también)
                bool isGrandFinal = match.Phase.Name.Contains("FINAL", StringComparison.OrdinalIgnoreCase);

                if (isGrandFinal && match.WinnerId.HasValue)
                {
                    var champion = match.WinnerId == match.LocalTeamId ? match.LocalTeam : match.VisitorTeam;

                    // Emitimos un evento especial para TODO el Hub o la Competición
                    await _hubContext.Clients.All.SendAsync("ReceiveChampion", new
                    {
                        competitionId = match.Phase.CompetitionId,
                        championName = champion.Name,
                        championLogo = champion.LogoUrl,
                        score = $"{match.LocalScore} - {match.VisitorScore}",
                        message = $"¡FELICIDADES {champion.Name}! CAMPEÓN DE LA {match.Phase.Name.ToUpper()}"
                    });
                }

                // SignalR estándar de fin de partido
                await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMatchUpdate", new
                {
                    matchId = id,
                    status = "Finalizado",
                    winnerId = match.WinnerId,
                    finalScore = $"{match.LocalScore} - {match.VisitorScore}"
                });

                return Ok(new { message = "Partido finalizado y campeón detectado.", winnerId = match.WinnerId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
