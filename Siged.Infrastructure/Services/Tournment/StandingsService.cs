using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.DTOs.Tournaments.Standing;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;

namespace Siged.Infrastructure.Services.Tournment
{
    public class StandingsService
    {
        private readonly ApplicationDbContext _context;

        public StandingsService(ApplicationDbContext context) => _context = context;

        public async Task<List<TeamStandingDto>> GetStandingsByGroupAsync(Guid groupId)
        {
            // 1. Obtener equipos del grupo y partidos finalizados
            var teams = await _context.GroupTeams
                .Include(gt => gt.Team)
                .Where(gt => gt.GroupId == groupId)
                .Select(gt => gt.Team)
                .ToListAsync();

            var matches = await _context.Matches
                .Where(m => m.GroupId == groupId && m.Status == MatchStatus.Finalizado)
                .ToListAsync();

            var standings = teams.Select(team => new TeamStandingDto
            {
                TeamId = team.Id,
                TeamName = team.Name,
                LogoUrl = team.LogoUrl
            }).ToList();

            // 2. Procesar cada partido para sumar estadísticas
            foreach (var match in matches)
            {
                var local = standings.First(s => s.TeamId == match.LocalTeamId);
                var visitor = standings.First(s => s.TeamId == match.VisitorTeamId);

                local.MatchesPlayed++;
                visitor.MatchesPlayed++;
                local.GoalsFor += match.LocalScore;
                local.GoalsAgainst += match.VisitorScore;
                visitor.GoalsFor += match.VisitorScore;
                visitor.GoalsAgainst += match.LocalScore;

                if (match.LocalScore > match.VisitorScore)
                {
                    local.Won++; local.Points += 3;
                    visitor.Lost++;
                }
                else if (match.LocalScore < match.VisitorScore)
                {
                    visitor.Won++; visitor.Points += 3;
                    local.Lost++;
                }
                else
                {
                    local.Drawn++; local.Points += 1;
                    visitor.Drawn++; visitor.Points += 1;
                }
            }

            // 3. Ordenar por Puntos, luego Diferencia de Goles, luego Goles a Favor
            return standings
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDifference)
                .ThenByDescending(s => s.GoalsFor)
                .ToList();
        }
    }
}
