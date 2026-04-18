using Siged.Domain.Constants;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Infrastructure.Services.Tournment
{
    public class DisciplineRuleService
    {
        public List<DisciplineRule> GetOfficialTemplate(string templateKey, Guid disciplineId)
        {
            // Verificamos si existe la plantilla (ej: FIFA_FUTSAL)
            if (!SportRulesTemplates.OfficialTemplates.TryGetValue(templateKey, out var template))
                return new List<DisciplineRule>();

            // Mapeamos las reglas de la plantilla a la entidad de la BD
            return template.Rules.Select(r => new DisciplineRule
            {
                DisciplineId = disciplineId,
                RuleKey = r.Key,
                RuleValue = r.Value
            }).ToList();
        }
    }
}