using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Application.DTOs.Tournaments.Match;

public class PatchMatchStatusDto
{
    public MatchStatus Status { get; set; }
}
