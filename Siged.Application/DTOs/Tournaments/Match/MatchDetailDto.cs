
namespace Siged.Application.DTOs.Tournaments.Match
{
    public class MatchDetailDto
    {
        public Guid Id { get; set; }
        public string LocalTeamName { get; set; } = string.Empty;
        public string? LocalTeamLogo { get; set; }
        public int LocalScore { get; set; }
        public string VisitorTeamName { get; set; } = string.Empty;
        public string? VisitorTeamLogo { get; set; }
        public int VisitorScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
    }
}
