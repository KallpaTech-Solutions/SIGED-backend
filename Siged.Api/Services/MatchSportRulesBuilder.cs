using Microsoft.EntityFrameworkCore;
using Siged.Domain.Constants;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Services
{
    public class MatchSportRulesBuilder
    {
        private readonly ApplicationDbContext _context;

        public MatchSportRulesBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, string>> BuildMapAsync(
            Guid competitionId,
            IEnumerable<DisciplineRule>? disciplineRules,
            string? templateKey)
        {
            var sportRules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (disciplineRules != null)
            {
                foreach (var r in disciplineRules)
                {
                    if (string.IsNullOrWhiteSpace(r.RuleKey)) continue;
                    sportRules[r.RuleKey] = r.RuleValue ?? "";
                }
            }

            var competitionRuleRows = await _context.CompetitionRules
                .AsNoTracking()
                .Where(r => r.CompetitionId == competitionId)
                .ToListAsync();
            foreach (var r in competitionRuleRows)
            {
                if (string.IsNullOrWhiteSpace(r.RuleKey)) continue;
                sportRules[r.RuleKey] = r.RuleValue ?? "";
            }

            if (!string.IsNullOrWhiteSpace(templateKey))
            {
                foreach (var kv in SportRulesTemplates.OfficialTemplates)
                {
                    if (!string.Equals(kv.Key, templateKey, StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (var rule in kv.Value.Rules)
                    {
                        if (!sportRules.ContainsKey(rule.Key))
                            sportRules[rule.Key] = rule.Value;
                    }
                    break;
                }
            }

            return sportRules;
        }
    }
}
