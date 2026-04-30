namespace Siged.Application.DTOs.Tournaments.Match;

/// <summary>
/// Tanda de penales (goles convertidos en definición) para desempatar eliminatorias con empate global.
/// </summary>
public sealed class PatchPenaltyScoreDto
{
    public int LocalPenaltyScore { get; set; }
    public int VisitorPenaltyScore { get; set; }
}
