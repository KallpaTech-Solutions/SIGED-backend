using System;
using System.Collections.Generic;
using System.Linq;
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
            var sourcePhase = await _context.Phases
                .FirstOrDefaultAsync(p => p.Id == dto.SourcePhaseId && p.CompetitionId == dto.CompetitionId)
                ?? throw new InvalidOperationException("Fase origen no encontrada o no pertenece a esta competencia.");

            if (sourcePhase.IsDirectElimination)
                throw new InvalidOperationException("La fase origen debe ser de grupos (no eliminatoria).");

            if (await _context.Phases.AnyAsync(p => p.CompetitionId == dto.CompetitionId && p.IsDirectElimination))
                throw new InvalidOperationException(
                    "Ya existe una fase eliminatoria. Usá «Promover ganadores» para la siguiente ronda.");

            var groups = await _context.Groups
                .Where(g => g.PhaseId == dto.SourcePhaseId)
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (groups.Count == 0)
                throw new InvalidOperationException("La fase origen no tiene grupos.");

            var allQualified = new List<(Guid TeamId, int Rank, string GroupName)>();

            foreach (var group in groups)
            {
                var standings = await _standingsService.GetStandingsByGroupAsync(group.Id);
                var qualifiedInGroup = standings.Take(group.QualifiedCount).ToList();

                for (int i = 0; i < qualifiedInGroup.Count; i++)
                {
                    allQualified.Add((qualifiedInGroup[i].TeamId, i + 1, group.Name));
                }
            }

            if (allQualified.Count < 2)
                throw new InvalidOperationException("Se necesitan al menos 2 clasificados para armar la eliminatoria.");

            var qualifiedSet = allQualified.Select(q => q.TeamId).ToHashSet();

            var nextOrder = await _context.Phases
                .Where(p => p.CompetitionId == dto.CompetitionId)
                .Select(p => (int?)p.Order)
                .MaxAsync() ?? 0;

            var competition = await _context.Competitions.FindAsync(dto.CompetitionId)
                ?? throw new InvalidOperationException("Competición no encontrada.");

            var newPhase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.NewPhaseName,
                Type = PhaseType.EliminacionSimple,
                IsDirectElimination = true,
                IsDoubleLeg = dto.IsDoubleLeg,
                Order = nextOrder + 1
            };
            _context.Phases.Add(newPhase);

            var bracketGroup = new Group { Phase = newPhase, Name = "Llave Principal", QualifiedCount = 1 };
            _context.Groups.Add(bracketGroup);

            await _context.SaveChangesAsync();

            var journal = new Journal
            {
                GroupId = bracketGroup.Id,
                PhaseId = newPhase.Id,
                Name = "Partidos de " + dto.NewPhaseName,
                Sequence = 1,
                IsActive = true,
                ScheduledDate = DateTime.UtcNow
            };
            _context.Journals.Add(journal);

            List<(Guid LocalTeamId, Guid VisitorTeamId)> pairings;

            var manual = dto.ManualPairings?.Where(p => p.LocalTeamId != Guid.Empty && p.VisitorTeamId != Guid.Empty).ToList();
            if (manual is { Count: > 0 })
            {
                if (allQualified.Count % 2 == 1)
                    throw new InvalidOperationException(
                        "Con cantidad impar de clasificados no se pueden definir solo cruces manuales pareados; usá automático (incluye tanda libre) o ajustá cupos por grupo.");

                ValidateManualPairings(manual, qualifiedSet, allQualified.Count);
                pairings = manual.Select(p => (p.LocalTeamId, p.VisitorTeamId)).ToList();
            }
            else
            {
                pairings = new List<(Guid, Guid)>();
                var n = allQualified.Count;
                for (int i = 0; i < n / 2; i++)
                {
                    var local = allQualified[i].TeamId;
                    var visitor = allQualified[n - 1 - i].TeamId;
                    pairings.Add((local, visitor));
                }
            }

            foreach (var (localId, visitorId) in pairings)
            {
                _context.Matches.Add(new Match
                {
                    Journal = journal,
                    LocalTeamId = localId,
                    VisitorTeamId = visitorId,
                    Status = MatchStatus.Programado,
                    GroupId = bracketGroup.Id,
                    PhaseId = newPhase.Id,
                    DisciplineId = competition.DisciplineId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            if (allQualified.Count % 2 == 1 && (manual == null || manual.Count == 0))
            {
                var paired = pairings.SelectMany(p => new[] { p.LocalTeamId, p.VisitorTeamId }).ToHashSet();
                var byeTeam = allQualified.Select(q => q.TeamId).First(id => !paired.Contains(id));
                _context.Matches.Add(new Match
                {
                    Journal = journal,
                    LocalTeamId = byeTeam,
                    VisitorTeamId = null,
                    Status = MatchStatus.Finalizado,
                    WinnerId = byeTeam,
                    LocalScore = 1,
                    VisitorScore = 0,
                    Note = "Pasa libre (clasificado impar en cruces automáticos)",
                    GroupId = bracketGroup.Id,
                    PhaseId = newPhase.Id,
                    DisciplineId = competition.DisciplineId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
        }

        private static void ValidateManualPairings(
            List<PlayoffManualPairingDto> manual,
            HashSet<Guid> qualifiedSet,
            int totalQualified)
        {
            foreach (var p in manual)
            {
                if (p.LocalTeamId == p.VisitorTeamId)
                    throw new InvalidOperationException("Un partido no puede tener el mismo equipo como local y visitante.");

                if (!qualifiedSet.Contains(p.LocalTeamId) || !qualifiedSet.Contains(p.VisitorTeamId))
                    throw new InvalidOperationException(
                        "Los cruces manuales solo pueden incluir equipos clasificados según la tabla actual.");
            }

            var used = new HashSet<Guid>();
            foreach (var p in manual)
            {
                if (!used.Add(p.LocalTeamId) || !used.Add(p.VisitorTeamId))
                    throw new InvalidOperationException("Cada clasificado solo puede aparecer en un cruce.");
            }

            if (used.Count != totalQualified)
                throw new InvalidOperationException(
                    $"Debés armar cruces para todos los clasificados ({totalQualified} equipos en {manual.Count} partidos).");
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
            var currentPhase = await _context.Phases.FindAsync(dto.CurrentPhaseId)
                ?? throw new InvalidOperationException("Fase actual no encontrada.");

            var maxOrder = await _context.Phases
                .Where(p => p.CompetitionId == dto.CompetitionId)
                .Select(p => (int?)p.Order)
                .MaxAsync() ?? 0;

            var nextPhase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.NextPhaseName,
                Type = PhaseType.EliminacionSimple,
                IsDirectElimination = true,
                Order = maxOrder + 1
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
