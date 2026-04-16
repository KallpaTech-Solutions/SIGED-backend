using Siged.Domain.Entities.Core.Tournaments.Enums;


namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Tournament
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? RulesUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Organizer { get; set; }
        public TournamentStatus Status { get; set; } = TournamentStatus.Borrador;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();
    }
}
