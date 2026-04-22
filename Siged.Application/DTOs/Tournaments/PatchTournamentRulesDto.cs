using Microsoft.AspNetCore.Http;

namespace Siged.Application.DTOs.Tournaments;

/// <summary>
/// Subida del PDF de reglamento del torneo (multipart). Agrupa el archivo para que Swagger genere el esquema correctamente.
/// </summary>
public class PatchTournamentRulesDto
{
    public IFormFile? RulesFile { get; set; }
}
