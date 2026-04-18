
namespace Siged.Domain.Entities.Core.Tournaments.Enums
{
    public enum ScoringType
    {
        // Fútbol, Futsal
        PointsBased,

        // Vóley, Tenis, Ping Pong
        SetsBased,

        // Básquet (2 pts por ganar, 1 por perder, sin empates)
        BasketBased,

        // Atletismo, Carreras (El menor tiempo gana)
        TimedRace
    }
}
