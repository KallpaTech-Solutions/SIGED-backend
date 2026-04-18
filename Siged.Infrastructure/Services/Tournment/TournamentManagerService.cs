using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;

namespace Siged.Infrastructure.Services.Tournment
{
    public class TournamentManagerService
    {
        private readonly ApplicationDbContext _context;

        public TournamentManagerService(ApplicationDbContext context) => _context = context;

        public async Task CloneRulesToCompetition(Guid competitionId, Guid disciplineId)
        {
            // 1. Obtener reglas base de la disciplina de forma asíncrona
            var defaultRules = await _context.DisciplineRules
                .Where(dr => dr.DisciplineId == disciplineId)
                .ToListAsync(); // Ahora sí funcionará

            // 2. Clonar a la tabla de la competencia
            var competitionRules = defaultRules.Select(dr => new CompetitionRule
            {
                CompetitionId = competitionId,
                RuleKey = dr.RuleKey,
                RuleValue = dr.RuleValue
            }).ToList();

            _context.CompetitionRules.AddRange(competitionRules);
            await _context.SaveChangesAsync();
        }
    }
}