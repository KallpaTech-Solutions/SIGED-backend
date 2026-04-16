
namespace Siged.Application.DTOs.Tournaments.Standing
{
    public class TeamStandingDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int MatchesPlayed { get; set; } // PJ
        public int Won { get; set; }           // PG
        public int Drawn { get; set; }         // PE
        public int Lost { get; set; }          // PP
        public int GoalsFor { get; set; }      // GF
        public int GoalsAgainst { get; set; }  // GC
        public int GoalDifference => GoalsFor - GoalsAgainst; // DG
        public int Points { get; set; }        // Pts
    }
}
