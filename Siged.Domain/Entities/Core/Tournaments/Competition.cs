using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class Competition
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relación con el Torneo (ej: Olimpiadas 2026)
        public Guid TournamentId { get; set; }
        public virtual Tournament Tournament { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        // Relación con el Deporte (ej: Fútbol)
        public Guid DisciplineId { get; set; }
        public virtual Discipline Discipline { get; set; } = null!;
        public virtual ICollection<CompetitionTeam> CompetitionTeams { get; set; } = new List<CompetitionTeam>();

        public Gender Gender { get; set; }
        public string? CategoryName { get; set; } // Ej: "Libre", "Inter-Escuelas"

        // Una competición tiene sus propias fases y partidos
        public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();

        /// <summary>Solo salida JSON (GET): opciones usadas al generar el formato inicial.</summary>
        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? FormatSetup { get; set; }

    }
}
