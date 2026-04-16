using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Entities.Core.Tournaments
{
    public class MatchEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid MatchId { get; set; }
        public virtual Match Match { get; set; } = null!;
        public int Minute { get; set; } // Min 15, 45, etc.
        public MatchEventType Type { get; set; } // Gol, Tarjeta Amarilla, Roja

        public Guid TeamId { get; set; } // ¿Quién lo hizo?
        public Guid? PlayerId { get; set; } // ¿Qué jugador lo hizo? (Opcional)
        public virtual Player? Player { get; set; }

        public string? Note { get; set; } // Ej: "Gol de cabeza"

        public int Value { get; set; } // 1 para gol, 3 para triple
        public int Period { get; set; } // 1T, 2T, Set 1, etc.
    }
}
