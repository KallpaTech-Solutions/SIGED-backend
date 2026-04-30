using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments;

public class MatchLineupPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchLineupId { get; set; }
    public virtual MatchLineup MatchLineup { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;

    public MatchLineupPlayerRole Role { get; set; }
    public int? ShirtNumber { get; set; }
    public PlayerPosition Position { get; set; } = PlayerPosition.None;
    public bool IsCaptain { get; set; }
    public bool IsGoalkeeper { get; set; }
    public string? Observation { get; set; }
}
