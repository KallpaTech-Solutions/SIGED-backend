using Siged.Domain.Entities.Core.Tournaments.Enums;

namespace Siged.Domain.Constants
{
    public static class SportRulesTemplates
    {
        public static readonly Dictionary<string, (ScoringType Type, string Name, List<(string Key, string Value, string Description)> Rules)> OfficialTemplates =
            new()
            {
                ["FIFA_FOOTBALL"] = (
                    ScoringType.PointsBased,
                    "Fútbol (Reglamento FIFA)",
                    new() {
                        ("POINTS_WIN", "3", "Puntos por victoria"),
                        ("POINTS_DRAW", "1", "Puntos por empate"),
                        ("POINTS_LOSS", "0", "Puntos por derrota"),
                        ("PERIODS_COUNT", "2", "Número de tiempos"),
                        ("PERIOD_DURATION", "45", "Duración de cada tiempo (min)"),
                        ("HAS_DRAW", "true", "Permite empates en fase de grupos"),
                        ("MAX_SUBSTITUTIONS", "5", "Máximo de cambios permitidos")
                    }
                ),
                ["FIFA_FUTSAL"] = (
                    ScoringType.PointsBased,
                    "Futsal (Reglamento FIFA)",
                    new() {
                        ("POINTS_WIN", "3", "Puntos por victoria"),
                        ("POINTS_DRAW", "1", "Puntos por empate"),
                        ("PERIODS_COUNT", "2", "Número de tiempos"),
                        ("PERIOD_DURATION", "20", "Duración de cada tiempo (min cronometrados)"),
                        ("FOUL_LIMIT", "5", "Límite de faltas para tiro libre directo"),
                        ("TIMEOUTS_PER_PERIOD", "1", "Tiempos muertos por equipo/periodo")
                    }
                ),
                ["FIVB_VOLLEYBALL"] = (
                    ScoringType.SetsBased,
                    "Voleibol (Reglamento FIVB)",
                    new() {
                        ("MAX_SETS", "5", "Máximo de sets (gana quien gane 3)"),
                        ("POINTS_REGULAR_SET", "25", "Puntos para ganar set normal"),
                        ("POINTS_FINAL_SET", "15", "Puntos para el 5to set (Tie-break)"),
                        ("MIN_POINTS_DIFF", "2", "Diferencia mínima para cerrar set"),
                        ("POINTS_3_0_OR_3_1", "3", "Puntos en tabla por ganar 3-0 o 3-1"),
                        ("POINTS_3_2", "2", "Puntos en tabla por ganar 3-2"),
                        ("POINTS_2_3", "1", "Puntos en tabla por perder 2-3")
                    }
                ),
                ["FIBA_BASKETBALL"] = (
                    ScoringType.BasketBased,
                    "Básquetbol (Reglamento FIBA)",
                    new() {
                        ("POINTS_WIN", "2", "Puntos por victoria"),
                        ("POINTS_LOSS", "1", "Puntos por derrota (presentación)"),
                        ("PERIODS_COUNT", "4", "Número de cuartos"),
                        ("PERIOD_DURATION", "10", "Duración de cada cuarto (min)"),
                        ("HAS_DRAW", "false", "No se permite el empate"),
                        ("OVERTIME_DURATION", "5", "Duración de tiempo extra")
                    }
                )
            };
    }
}