using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments;

public class PlayerSanction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PlayerId { get; set; }
    public virtual Player Player { get; set; } = null!;

    public Guid? CompetitionId { get; set; }
    public virtual Competition? Competition { get; set; }

    public Guid? TeamId { get; set; }
    public virtual Team? Team { get; set; }

    public Guid? MatchId { get; set; }
    public virtual Match? Match { get; set; }

    public Guid? MatchEventId { get; set; }
    public virtual MatchEvent? MatchEvent { get; set; }

    public PlayerSanctionType Type { get; set; } = PlayerSanctionType.Matches;
    public string Reason { get; set; } = string.Empty;
    public int? MatchesCount { get; set; }
    public int? PhasesCount { get; set; }
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedByUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? LiftedByUsuarioId { get; set; }
    public DateTime? LiftedAt { get; set; }
    public string? Observation { get; set; }
}
