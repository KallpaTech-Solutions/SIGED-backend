using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments;

public class MatchLineup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }
    public virtual Match Match { get; set; } = null!;

    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public int? SubmittedByUsuarioId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public MatchLineupStatus Status { get; set; } = MatchLineupStatus.Draft;
    public string? Observation { get; set; }

    public virtual ICollection<MatchLineupPlayer> Players { get; set; } = new List<MatchLineupPlayer>();
}
