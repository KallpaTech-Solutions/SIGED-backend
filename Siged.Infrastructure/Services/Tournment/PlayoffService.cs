using Siged.Application.DTOs.Tournaments.Playoff;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Siged.Infrastructure.Services.Tournment
{
    public class PlayoffService
    {
        private readonly ApplicationDbContext _context;
        private readonly StandingsService _standingsService;

        public PlayoffService(ApplicationDbContext context, StandingsService standingsService)
        {
            _context = context;
            _standingsService = standingsService;
        }

        public async Task GenerateKnockoutFromGroups(GeneratePlayoffDto dto)
        {
            // 1. Obtener los grupos de la fase anterior
            var groups = await _context.Groups
                .Where(g => g.PhaseId == dto.SourcePhaseId)
                .OrderBy(g => g.Name)
                .ToListAsync();

            var allQualified = new List<(Guid TeamId, int Rank, string GroupName)>();

            // 2. Extraer los clasificados de cada grupo usando tu StandingsService
            foreach (var group in groups)
            {
                var standings = await _standingsService.GetStandingsByGroupAsync(group.Id);
                var qualifiedInGroup = standings.Take(group.QualifiedCount).ToList();

                for (int i = 0; i < qualifiedInGroup.Count; i++)
                {
                    allQualified.Add((qualifiedInGroup[i].TeamId, i + 1, group.Name));
                }
            }

            // 3. Crear la nueva Fase de Eliminación Directa
            var newPhase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.NewPhaseName,
                Type = PhaseType.EliminacionSimple,
                IsDirectElimination = true,
                Order = 2 // Suponiendo que la de grupos fue la 1
            };
            _context.Phases.Add(newPhase);

            // Creamos un "Grupo" ficticio para contener los partidos de la llave
            var bracketGroup = new Group { Phase = newPhase, Name = "Llave Principal", QualifiedCount = 1 };
            _context.Groups.Add(bracketGroup);

            await _context.SaveChangesAsync();

            // 4. Lógica de Cruce (1ero A vs 2do B, 1ero B vs 2do A...)
            var journal = new Journal { GroupId = bracketGroup.Id, Name = "Partidos de " + dto.NewPhaseName, Sequence = 1 };
            _context.Journals.Add(journal);

            for (int i = 0; i < allQualified.Count / 2; i++)
            {
                // Cruce simple: El primero de una lista con el último de la lista invertida
                // Esto funciona bien si tienes 2 grupos (1A vs 2B y 1B vs 2A)
                var local = allQualified[i];
                var visitor = allQualified[allQualified.Count - 1 - i];

                var match = new Match
                {
                    Journal = journal,
                    LocalTeamId = local.TeamId,
                    VisitorTeamId = visitor.TeamId,
                    Status = MatchStatus.Programado,
                    GroupId = bracketGroup.Id,
                    PhaseId = newPhase.Id,
                    DisciplineId = (await _context.Competitions.FindAsync(dto.CompetitionId))!.DisciplineId
                };
                _context.Matches.Add(match);
            }

            await _context.SaveChangesAsync();
        }

        public async Task PromoteWinnersToNextPhase(PromoteWinnersDto dto)
        {
            // 1. Obtener todos los partidos de la fase actual que tengan un ganador
            var matches = await _context.Matches
                .Where(m => m.PhaseId == dto.CurrentPhaseId && m.Status == MatchStatus.Finalizado)
                .OrderBy(m => m.Id) // Importante para mantener el orden de la llave
                .ToListAsync();

            var winners = matches
                .Where(m => m.WinnerId.HasValue)
                .Select(m => m.WinnerId!.Value)
                .ToList();

            if (winners.Count < 2)
                throw new Exception("No hay suficientes ganadores para generar la siguiente ronda.");

            // 2. Crear la siguiente Fase (Ej. Semifinal)
            var currentPhase = await _context.Phases.FindAsync(dto.CurrentPhaseId);
            var nextPhase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.NextPhaseName,
                Type = PhaseType.EliminacionSimple,
                IsDirectElimination = true,
                Order = (currentPhase?.Order ?? 1) + 1
            };
            _context.Phases.Add(nextPhase);

            var bracketGroup = new Group { Phase = nextPhase, Name = "Llave " + dto.NextPhaseName };
            _context.Groups.Add(bracketGroup);
            await _context.SaveChangesAsync();

            var journal = new Journal { 
                GroupId = bracketGroup.Id, 
                PhaseId = nextPhase.Id,
                Name = dto.NextPhaseName, 
                Sequence = 1,
                ScheduledDate = DateTime.UtcNow
            };
            _context.Journals.Add(journal);

            // 3. Emparejamiento automático: Ganador M1 vs Ganador M2, M3 vs M4...
            for (int i = 0; i < winners.Count; i += 2)
            {
                if (i + 1 < winners.Count)
                {
                    var match = new Match
                    {
                        Journal = journal,
                        LocalTeamId = winners[i],
                        VisitorTeamId = winners[i + 1],
                        Status = MatchStatus.Programado,
                        GroupId = bracketGroup.Id,
                        PhaseId = nextPhase.Id,
                        DisciplineId = (await _context.Competitions.FindAsync(dto.CompetitionId))!.DisciplineId
                    };
                    _context.Matches.Add(match);
                }
                else
                {
                    // Si queda uno solo (número impar de ganadores), pasa directo (BYE)
                    var matchFree = new Match
                    {
                        Journal = journal,
                        LocalTeamId = winners[i],
                        VisitorTeamId = null,
                        Status = MatchStatus.Finalizado,
                        WinnerId = winners[i],
                        LocalScore = 1,
                        VisitorScore = 0,
                        Note = "Avanza por ser el ganador restante",
                        GroupId = bracketGroup.Id,
                        PhaseId = nextPhase.Id,
                        DisciplineId = (await _context.Competitions.FindAsync(dto.CompetitionId))!.DisciplineId
                    };
                    _context.Matches.Add(matchFree);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Guid> GenerateDirectKnockout(GenerateDirectKnockoutDto dto)
        {
            var competition = await _context.Competitions.FindAsync(dto.CompetitionId);
            if (competition == null) throw new Exception("Competición no encontrada.");

            // 1. Mezclar equipos si es aleatorio
            if (dto.IsRandom)
            {
                var rng = new Random();
                dto.TeamIds = dto.TeamIds.OrderBy(x => rng.Next()).ToList();
            }

            // 2. Crear la Fase
            var newPhase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.PhaseName,
                Type = PhaseType.EliminacionSimple,
                IsDirectElimination = true,
                Order = 1                
            };
            _context.Phases.Add(newPhase);

            // Creamos el grupo de la llave
            var bracketGroup = new Group
            {
                Phase = newPhase,
                Name = "Llave " + dto.PhaseName,
                QualifiedCount = 1
            };
            _context.Groups.Add(bracketGroup);

            // PRIMER GUARDADO: Si falla aquí, el problema es Phase o Group
            await _context.SaveChangesAsync();

            var journal = new Journal
            {
                GroupId = bracketGroup.Id,
                PhaseId = newPhase.Id,
                Name = dto.PhaseName,
                Sequence = 1,
                IsActive = true, // 🚀 Probablemente requerido
                ScheduledDate = DateTime.UtcNow
            };
            _context.Journals.Add(journal);

            // 3. Emparejamiento
            for (int i = 0; i < dto.TeamIds.Count; i += 2)
            {
                bool hasVisitor = (i + 1 < dto.TeamIds.Count);

                var match = new Match
                {
                    Journal = journal,
                    LocalTeamId = dto.TeamIds[i],
                    VisitorTeamId = hasVisitor ? dto.TeamIds[i + 1] : null,
                    Status = hasVisitor ? MatchStatus.Programado : MatchStatus.Finalizado,
                    GroupId = bracketGroup.Id,
                    PhaseId = newPhase.Id,
                    DisciplineId = competition.DisciplineId,
                    CreatedAt = DateTime.UtcNow, // 🚀 POSTGRES suele exigir esto
                    IsActive = true
                };

                if (!hasVisitor)
                {
                    // Lógica de BYE
                    match.WinnerId = dto.TeamIds[i];
                    match.LocalScore = 1;
                    match.VisitorScore = 0;
                    match.Note = "Pasa libre por sorteo";
                }

                _context.Matches.Add(match);
            }

            // SEGUNDO GUARDADO: Si falla aquí, el problema es Journal o Match
            await _context.SaveChangesAsync();
            return newPhase.Id;
        }
    }
}
