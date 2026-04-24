namespace Siged.Application.DTOs.Tournaments.Match;

/// <summary>
/// Pausar o reanudar el cronómetro del periodo (independiente de pausar la transmisión en vitrina).
/// </summary>
public class PatchMatchClockDto
{
    /// <summary>true = congelar (flush ancla → acumulado); false = reanudar tramo (ancla UTC ahora si falta).</summary>
    public bool Paused { get; set; }
}
