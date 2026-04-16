using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;

namespace Siged.Infrastructure.Services.Tournment
{
    public class FixtureService
    {
        private readonly ApplicationDbContext _context;

        public FixtureService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateRoundRobin(Guid groupId)
        {
            // 1. Obtener el grupo con su Fase y Competición para sacar los IDs necesarios
            var groupInfo = await _context.Groups
                .Include(g => g.Phase)
                    .ThenInclude(p => p.Competition)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (groupInfo == null) return;

            var phaseId = groupInfo.PhaseId;
            var disciplineId = groupInfo.Phase.Competition.DisciplineId;

            // 2. Obtener equipos del grupo
            var groupTeams = await _context.GroupTeams
                .Where(gt => gt.GroupId == groupId)
                .Select(gt => gt.TeamId)
                .ToListAsync();

            if (groupTeams.Count < 2) return;

            // Manejo de impares
            if (groupTeams.Count % 2 != 0) groupTeams.Add(Guid.Empty);

            int numTeams = groupTeams.Count;
            int numDays = numTeams - 1;
            int halfSize = numTeams / 2;
            List<Guid> teams = new List<Guid>(groupTeams);

            for (int day = 0; day < numDays; day++)
            {
                // Crear la Jornada (Journal) con los nuevos campos
                var journal = new Journal
                {
                    GroupId = groupId,
                    PhaseId = phaseId, // 👈 Ahora lo pide tu entidad
                    Name = $"Fecha {day + 1}",
                    Sequence = day + 1,
                    IsActive = true
                };
                _context.Journals.Add(journal);
                await _context.SaveChangesAsync();

                for (int i = 0; i < halfSize; i++)
                {
                    Guid homeId = teams[i];
                    Guid awayId = teams[numTeams - 1 - i];

                    if (homeId != Guid.Empty && awayId != Guid.Empty)
                    {
                        var match = new Match
                        {
                            JournalId = journal.Id,
                            PhaseId = phaseId,          
                            DisciplineId = disciplineId, 
                            GroupId = groupId,           
                            LocalTeamId = homeId,
                            VisitorTeamId = awayId,
                            Status = MatchStatus.Programado,
                            IsActive = true
                        };
                        _context.Matches.Add(match);
                    }
                }

                // Rotación Berger
                Guid lastTeam = teams[numTeams - 1];
                teams.RemoveAt(numTeams - 1);
                teams.Insert(1, lastTeam);
            }
            await _context.SaveChangesAsync();
        }
    }
}
