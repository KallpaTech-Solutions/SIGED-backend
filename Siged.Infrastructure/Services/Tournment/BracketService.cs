using System;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Bracket;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;

namespace Siged.Infrastructure.Services.Tournment
{
    public class BracketService
    {
        private readonly ApplicationDbContext _context;

        public BracketService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BracketDto> GetBracketByPhaseAsync(Guid phaseId)
        {
            // 1. Traemos la fase con sus hijos (sin sorting aquí para evitar líos)
            var phase = await _context.Phases
                .Include(p => p.Journals)
                    .ThenInclude(j => j.Matches)
                        .ThenInclude(m => m.LocalTeam)
                .Include(p => p.Journals)
                    .ThenInclude(j => j.Matches)
                        .ThenInclude(m => m.VisitorTeam)
                .Include(p => p.Journals)
                    .ThenInclude(j => j.Matches)
                        .ThenInclude(m => m.Venue)
                .FirstOrDefaultAsync(p => p.Id == phaseId);

            if (phase == null) return new BracketDto();

            // 2. Proyectamos y ordenamos aquí (C# es más eficiente ordenando en el Select para DTOs)
            var response = new BracketDto
            {
                PhaseId = phase.Id,
                PhaseName = phase.Name,
                Rounds = phase.Journals
                    .OrderBy(j => j.Sequence) // 👈 Ordenamos las jornadas aquí
                    .Select(j => new BracketRoundDto
                    {
                        Title = j.Name,
                        Matches = j.Matches
                            .OrderBy(m => m.CreatedAt) // 👈 O por el criterio que prefieras
                            .Select(m => new BracketMatchDto
                            {
                                MatchId = m.Id,
                                LocalName = TeamLabel(m.LocalTeam?.Name, m.Status),
                                VisitorName = TeamLabel(m.VisitorTeam?.Name, m.Status),
                                LocalScore = m.LocalScore,
                                VisitorScore = m.VisitorScore,
                                LocalPenaltyScore = m.LocalPenaltyScore,
                                VisitorPenaltyScore = m.VisitorPenaltyScore,
                                WinnerId = m.WinnerId,
                                Status = m.Status.ToString(),
                                Note = m.Note,
                                ScheduledAt = m.ScheduledAt == default ? null : m.ScheduledAt,
                                VenueName = m.Venue?.Name
                            }).ToList()
                    }).ToList()
            };

            return response;
        }

        /// <summary>
        /// "Por definir" solo cuando el partido sigue abierto y falta asignar rival.
        /// Si ya está finalizado y falta nombre (hueco/W.O.), se muestra "—" para no contradecir el estado.
        /// </summary>
        private static string TeamLabel(string? name, MatchStatus status)
        {
            if (!string.IsNullOrWhiteSpace(name))
                return name;
            if (status == MatchStatus.Finalizado)
                return "—";
            return "Por definir";
        }
    }
}
