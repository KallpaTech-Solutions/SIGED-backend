namespace Siged.Application.DTOs.Tournaments;

/// <summary>Mesa: fijar o limpiar campeón de la competencia (p. ej. final + 3.er puesto en misma jornada).</summary>
public sealed class SetCompetitionChampionDto
{
    /// <summary>Null borra el campeón registrado.</summary>
    public Guid? ChampionTeamId { get; set; }
}
