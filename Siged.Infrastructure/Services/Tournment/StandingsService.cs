using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Standing;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;

namespace Siged.Infrastructure.Services.Tournment
{
    public class StandingsService
    {
        private readonly ApplicationDbContext _context;

        public StandingsService(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Recalcula y PERSISTE los puntos en la tabla GroupTeam.
        /// Se debe llamar cada vez que un partido finaliza.
        /// </summary>
        public async Task UpdateGroupStandingsAsync(Guid groupId)
        {
            // 1. Obtener los equipos y los partidos finalizados
            var groupTeams = await _context.GroupTeams
                .Where(gt => gt.GroupId == groupId)
                .ToListAsync();

            var matches = await _context.Matches
                .Include(m => m.Journal)
                    .ThenInclude(j => j.Group)
                        .ThenInclude(g => g.Phase)
                .Where(m => m.GroupId == groupId && m.Status == MatchStatus.Finalizado)
                .ToListAsync();

            // 🛡️ REGLA MAESTRA: Obtener los puntos por victoria/empate de la competencia
            // Si no existen, usamos 3 y 1 por defecto.
            var competitionId = await _context.Groups
                .Where(g => g.Id == groupId)
                .Select(g => g.Phase.CompetitionId)
                .FirstOrDefaultAsync();

            var rules = await _context.CompetitionRules
                .Where(cr => cr.CompetitionId == competitionId)
                .ToListAsync();

            int ptsWin = int.Parse(rules.FirstOrDefault(r => r.RuleKey == "POINTS_WIN")?.RuleValue ?? "3");
            int ptsDraw = int.Parse(rules.FirstOrDefault(r => r.RuleKey == "POINTS_DRAW")?.RuleValue ?? "1");

            // 2. Reiniciar estadísticas antes de recalcular
            foreach (var gt in groupTeams)
            {
                gt.MatchesPlayed = 0; gt.MatchesWon = 0; gt.MatchesDrawn = 0; gt.MatchesLost = 0;
                gt.GoalsFor = 0; gt.GoalsAgainst = 0; gt.Points = 0;
            }

            // 3. Procesar partidos
            foreach (var match in matches)
            {
                var local = groupTeams.FirstOrDefault(gt => gt.TeamId == match.LocalTeamId);
                var visitor = groupTeams.FirstOrDefault(gt => gt.TeamId == match.VisitorTeamId);

                if (local == null || visitor == null) continue;

                local.MatchesPlayed++;
                visitor.MatchesPlayed++;
                local.GoalsFor += match.LocalScore;
                local.GoalsAgainst += match.VisitorScore;
                visitor.GoalsFor += match.VisitorScore;
                visitor.GoalsAgainst += match.LocalScore;

                if (match.LocalScore > match.VisitorScore)
                {
                    local.MatchesWon++; local.Points += ptsWin;
                    visitor.MatchesLost++;
                }
                else if (match.LocalScore < match.VisitorScore)
                {
                    visitor.MatchesWon++; visitor.Points += ptsWin;
                    local.MatchesLost++;
                }
                else
                {
                    local.MatchesDrawn++; local.Points += ptsDraw;
                    visitor.MatchesDrawn++; visitor.Points += ptsDraw;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Obtiene la tabla de posiciones ya calculada para mostrar en React.
        /// </summary>
        public async Task<List<TeamStandingDto>> GetStandingsByGroupAsync(Guid groupId)
        {
            return await _context.GroupTeams
                .Include(gt => gt.Team)
                .Where(gt => gt.GroupId == groupId)
                .OrderByDescending(gt => gt.Points)
                .ThenByDescending(gt => gt.GoalsFor - gt.GoalsAgainst) // Diferencia
                .ThenByDescending(gt => gt.GoalsFor)
                .Select(gt => new TeamStandingDto
                {
                    TeamId = gt.TeamId,
                    TeamName = gt.Team.Name,
                    LogoUrl = gt.Team.LogoUrl,
                    MatchesPlayed = gt.MatchesPlayed,
                    Won = gt.MatchesWon,
                    Drawn = gt.MatchesDrawn,
                    Lost = gt.MatchesLost,
                    GoalsFor = gt.GoalsFor,
                    GoalsAgainst = gt.GoalsAgainst,
                    Points = gt.Points
                })
                .ToListAsync();
        }
    }
}