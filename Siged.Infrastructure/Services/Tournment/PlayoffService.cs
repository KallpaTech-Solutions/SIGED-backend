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

            if (allQualified.Count % 2 == 1)
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
                    Note = "Pasa libre (clasificado impar)",
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

            var expectedUsed = totalQualified % 2 == 0 ? totalQualified : totalQualified - 1;
            if (used.Count != expectedUsed)
                throw new InvalidOperationException(
                    totalQualified % 2 == 0
                        ? $"Debés armar cruces para todos los clasificados ({totalQualified} equipos en {manual.Count} partidos)."
                        : $"Con cantidad impar ({totalQualified}), definí cruces para {expectedUsed} equipos; el restante pasa libre automáticamente.");
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

            var teamIds = dto.TeamIds.Distinct().ToList();
            if (teamIds.Count < 2)
                throw new InvalidOperationException("Se necesitan al menos 2 equipos para armar una eliminatoria.");

            var manual = dto.ManualPairings?
                .Where(p => p.LocalTeamId != Guid.Empty && p.VisitorTeamId != Guid.Empty)
                .ToList();
            var useManual = manual is { Count: > 0 };

            // 1. Definir orden/emparejamiento base
            if (!useManual && dto.IsRandom)
            {
                var rng = new Random();
                teamIds = teamIds.OrderBy(x => rng.Next()).ToList();
            }

            // 2. Crear la Fase
            var maxOrder = await _context.Phases
                .Where(p => p.CompetitionId == dto.CompetitionId)
                .Select(p => (int?)p.Order)
                .MaxAsync() ?? 0;

            var newPhase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.PhaseName,
                Type = PhaseType.EliminacionSimple,
                IsDirectElimination = true,
                Order = maxOrder + 1
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

            // 3. Crear rondas (journals) y toda la llave hasta la final
            var bracketSize = NextPowerOfTwo(teamIds.Count);
            var roundsCount = (int)Math.Log2(bracketSize);
            var journals = new List<Journal>();
            for (var seq = 1; seq <= roundsCount; seq++)
            {
                journals.Add(new Journal
                {
                    GroupId = bracketGroup.Id,
                    PhaseId = newPhase.Id,
                    Name = RoundName(seq, roundsCount, dto.PhaseName),
                    Sequence = seq,
                    IsActive = true,
                    ScheduledDate = DateTime.UtcNow
                });
            }
            _context.Journals.AddRange(journals);
            await _context.SaveChangesAsync();

            // Slots base de la primera ronda (completa con null para "bye/por definir")
            var slots = Enumerable.Repeat<Guid?>(null, bracketSize).ToList();
            if (useManual)
            {
                ValidateDirectManualPairings(manual!, teamIds);
                var i = 0;
                foreach (var p in manual!)
                {
                    slots[i++] = p.LocalTeamId;
                    slots[i++] = p.VisitorTeamId;
                }
                if (teamIds.Count % 2 == 1)
                {
                    var used = manual!
                        .SelectMany(x => new[] { x.LocalTeamId, x.VisitorTeamId })
                        .ToHashSet();
                    var byeTeam = teamIds.First(id => !used.Contains(id));
                    slots[i] = byeTeam;
                }
            }
            else
            {
                for (var i = 0; i < teamIds.Count; i++)
                    slots[i] = teamIds[i];
            }

            // Primera ronda: cruces reales + byes
            var roundWinners = new List<Guid?>();
            for (var i = 0; i < bracketSize; i += 2)
            {
                var local = slots[i];
                var visitor = slots[i + 1];
                var match = new Match
                {
                    JournalId = journals[0].Id,
                    GroupId = bracketGroup.Id,
                    PhaseId = newPhase.Id,
                    DisciplineId = competition.DisciplineId,
                    LocalTeamId = local ?? visitor, // normaliza "bye" a local
                    VisitorTeamId = local.HasValue ? visitor : null,
                    Status = (local.HasValue ^ visitor.HasValue) ? MatchStatus.Finalizado : MatchStatus.Programado,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                if (local.HasValue ^ visitor.HasValue)
                {
                    match.WinnerId = local ?? visitor;
                    match.LocalScore = 1;
                    match.VisitorScore = 0;
                    match.Note = "Pasa libre por bye";
                    roundWinners.Add(match.WinnerId);
                }
                else
                {
                    roundWinners.Add(null);
                }
                _context.Matches.Add(match);
            }
            await _context.SaveChangesAsync();

            // Rondas siguientes: placeholders (si ambos ganadores por bye ya definidos, se autoasigna)
            for (var round = 2; round <= roundsCount; round++)
            {
                var winners = new List<Guid?>();
                var journal = journals[round - 1];
                for (var i = 0; i < roundWinners.Count; i += 2)
                {
                    var local = roundWinners[i];
                    var visitor = roundWinners[i + 1];
                    var match = new Match
                    {
                        JournalId = journal.Id,
                        GroupId = bracketGroup.Id,
                        PhaseId = newPhase.Id,
                        DisciplineId = competition.DisciplineId,
                        LocalTeamId = local,
                        VisitorTeamId = visitor,
                        // En rondas posteriores, un solo equipo asignado significa "espera rival";
                        // no debe auto-avanzar hasta que se complete el otro cruce.
                        Status = MatchStatus.Programado,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    winners.Add(null);
                    _context.Matches.Add(match);
                }
                await _context.SaveChangesAsync();
                roundWinners = winners;
            }

            await _context.SaveChangesAsync();
            return newPhase.Id;
        }

        private static void ValidateDirectManualPairings(
            List<PlayoffManualPairingDto> manual,
            List<Guid> teamIds)
        {
            var expected = teamIds.ToHashSet();
            var used = new HashSet<Guid>();

            foreach (var p in manual)
            {
                if (p.LocalTeamId == p.VisitorTeamId)
                    throw new InvalidOperationException("Un partido no puede tener el mismo equipo como local y visitante.");

                if (!expected.Contains(p.LocalTeamId) || !expected.Contains(p.VisitorTeamId))
                    throw new InvalidOperationException("Los cruces manuales contienen equipos fuera de la lista de inscritos.");

                if (!used.Add(p.LocalTeamId) || !used.Add(p.VisitorTeamId))
                    throw new InvalidOperationException("Cada equipo solo puede aparecer una vez en cruces manuales.");
            }

            var expectedUsed = expected.Count % 2 == 0 ? expected.Count : expected.Count - 1;
            if (used.Count != expectedUsed)
                throw new InvalidOperationException(
                    expected.Count % 2 == 0
                        ? $"Debés armar cruces para todos los equipos ({expected.Count} en total)."
                        : $"Con cantidad impar ({expected.Count}), definí cruces para {expectedUsed} equipos; el restante pasa libre automáticamente.");
        }

        private static int NextPowerOfTwo(int n)
        {
            var p = 1;
            while (p < n) p <<= 1;
            return p;
        }

        private static string RoundName(int sequence, int totalRounds, string phaseName)
        {
            if (totalRounds <= 1) return phaseName;
            if (sequence == totalRounds) return "Final";
            if (sequence == totalRounds - 1) return "Semifinales";
            if (sequence == totalRounds - 2) return "Cuartos de final";
            if (sequence == totalRounds - 3) return "Octavos de final";
            return $"Ronda {sequence}";
        }
    }
}
