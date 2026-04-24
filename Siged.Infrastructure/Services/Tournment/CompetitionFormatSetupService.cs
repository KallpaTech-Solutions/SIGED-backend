using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.DTOs.Tournaments.Playoff;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Infrastructure.Persistence;

namespace Siged.Infrastructure.Services.Tournment
{
    /// <summary>
    /// Armado inicial: grupos equilibrados (A,B,C…) o eliminación directa, más RR opcional por grupo.
    /// </summary>
    public class CompetitionFormatSetupService
    {
        private readonly ApplicationDbContext _context;
        private readonly PlayoffService _playoffService;
        private readonly FixtureService _fixtureService;

        public CompetitionFormatSetupService(
            ApplicationDbContext context,
            PlayoffService playoffService,
            FixtureService fixtureService)
        {
            _context = context;
            _playoffService = playoffService;
            _fixtureService = fixtureService;
        }

        /// <summary>
        /// Calcula tamaños de grupos: suman <paramref name="totalTeams"/>, cada uno ≤ <paramref name="maxPerGroup"/>,
        /// cantidad mínima de grupos (ej. 15 y máx 4 → 4+4+4+3).
        /// </summary>
        public static int[] ComputeBalancedGroupSizes(int totalTeams, int maxPerGroup)
        {
            if (totalTeams < 2)
                throw new ArgumentException("Se necesitan al menos 2 equipos.", nameof(totalTeams));
            if (maxPerGroup < 2)
                throw new ArgumentException("El máximo por grupo debe ser al menos 2.", nameof(maxPerGroup));

            var g = (int)Math.Ceiling((double)totalTeams / maxPerGroup);
            var q = totalTeams / g;
            var r = totalTeams % g;
            var sizes = new int[g];
            for (var i = 0; i < g; i++)
                sizes[i] = q + (i < r ? 1 : 0);

            if (sizes.Max() > maxPerGroup)
                throw new InvalidOperationException("No se pudo repartir equipos respetando el máximo por grupo.");

            return sizes;
        }

        private static string GroupLabel(int index)
        {
            if (index >= 0 && index < 26)
                return $"Grupo {(char)('A' + index)}";
            return $"Grupo {index + 1}";
        }

        public async Task<SetupCompetitionFormatResultDto> SetupAsync(
            Guid competitionId,
            SetupCompetitionFormatDto dto,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<string>();
            var teamIds = dto.TeamIds.Distinct().ToList();
            if (teamIds.Count < 2)
                throw new InvalidOperationException("Indicá al menos dos equipos distintos.");

            var competition = await _context.Competitions
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken)
                ?? throw new InvalidOperationException("Competencia no encontrada.");

            var inscribed = await _context.CompetitionTeams
                .AsNoTracking()
                .Where(ct => ct.CompetitionId == competitionId && teamIds.Contains(ct.TeamId))
                .Select(ct => ct.TeamId)
                .ToListAsync(cancellationToken);

            var missingInscription = teamIds.Except(inscribed).ToList();
            if (missingInscription.Count > 0)
                throw new InvalidOperationException(
                    $"Equipos no inscritos en esta competencia: {string.Join(", ", missingInscription)}");

            var inactive = await _context.Teams
                .AsNoTracking()
                .Where(t => teamIds.Contains(t.Id) && !t.IsActive)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
            if (inactive.Count > 0)
                throw new InvalidOperationException(
                    $"Equipos inactivos (no pueden usarse): {string.Join(", ", inactive)}");

            var hasPhases = await _context.Phases
                .AnyAsync(p => p.CompetitionId == competitionId, cancellationToken);
            if (hasPhases)
                throw new InvalidOperationException(
                    "Esta competencia ya tiene fases. El armado automático solo aplica sobre competencias sin fases. " +
                    "Usá la gestión manual o una competencia nueva.");

            if (dto.Mode == CompetitionFormatSetupMode.DirectElimination)
            {
                var phaseId = await _playoffService.GenerateDirectKnockout(new GenerateDirectKnockoutDto
                {
                    CompetitionId = competitionId,
                    PhaseName = dto.KnockoutPhaseName,
                    TeamIds = teamIds,
                    IsRandom = dto.KnockoutRandomSeed
                });

                await PersistFormatSetupSnapshotAsync(competitionId, cancellationToken,
                    (CompetitionFormatSetupSnapshotKeys.Mode, CompetitionFormatSetupMode.DirectElimination.ToString()),
                    (CompetitionFormatSetupSnapshotKeys.KnockoutPhaseName, dto.KnockoutPhaseName ?? ""),
                    (CompetitionFormatSetupSnapshotKeys.KnockoutRandom, dto.KnockoutRandomSeed ? "true" : "false"));

                var phase = await _context.Phases.AsNoTracking()
                    .FirstAsync(p => p.Id == phaseId, cancellationToken);

                messages.Add(dto.KnockoutRandomSeed
                    ? "Eliminatoria generada con orden de equipos aleatorio."
                    : "Eliminatoria generada respetando el orden de la lista de equipos (manual).");

                return new SetupCompetitionFormatResultDto
                {
                    CompetitionId = competitionId,
                    Mode = CompetitionFormatSetupMode.DirectElimination,
                    PhaseId = phaseId,
                    PhaseName = phase.Name,
                    Groups = await _context.Groups.AsNoTracking()
                        .Where(g => g.PhaseId == phaseId)
                        .Select(g => new SetupCompetitionFormatGroupResultDto
                        {
                            Id = g.Id,
                            Name = g.Name,
                            TeamCount = g.GroupTeams.Count,
                            QualifiedCount = g.QualifiedCount
                        })
                        .ToListAsync(cancellationToken),
                    Messages = messages
                };
            }

            // --- Grupos + round robin ---
            var sizes = ComputeBalancedGroupSizes(teamIds.Count, dto.MaxTeamsPerGroup);
            if (dto.QualifiedPerGroup < 1)
                throw new InvalidOperationException("Indicá al menos 1 clasificado por grupo.");
            if (dto.QualifiedPerGroup > sizes.Min())
                throw new InvalidOperationException(
                    $"No pueden clasificar {dto.QualifiedPerGroup} por grupo: el grupo más chico tiene {sizes.Min()} equipos.");

            var orderedTeams = new List<Guid>(teamIds);
            if (dto.ShuffleTeams)
            {
                var rng = new Random();
                orderedTeams = orderedTeams.OrderBy(_ => rng.Next()).ToList();
                messages.Add("Equipos mezclados aleatoriamente antes de formar grupos.");
            }

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            var nextOrder = await _context.Phases
                .Where(p => p.CompetitionId == competitionId)
                .Select(p => (int?)p.Order)
                .MaxAsync(cancellationToken) ?? 0;

            var phaseEntity = new Phase
            {
                CompetitionId = competitionId,
                Name = dto.GroupPhaseName,
                Type = PhaseType.RoundRobin,
                IsDirectElimination = false,
                Order = nextOrder + 1
            };
            _context.Phases.Add(phaseEntity);
            await _context.SaveChangesAsync(cancellationToken);

            var groupResults = new List<SetupCompetitionFormatGroupResultDto>();
            var teamIndex = 0;

            for (var gi = 0; gi < sizes.Length; gi++)
            {
                var size = sizes[gi];
                var group = new Group
                {
                    PhaseId = phaseEntity.Id,
                    Name = GroupLabel(gi),
                    QualifiedCount = dto.QualifiedPerGroup
                };
                _context.Groups.Add(group);
                await _context.SaveChangesAsync(cancellationToken);

                for (var j = 0; j < size; j++)
                {
                    _context.GroupTeams.Add(new GroupTeam
                    {
                        GroupId = group.Id,
                        TeamId = orderedTeams[teamIndex++]
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
                groupResults.Add(new SetupCompetitionFormatGroupResultDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    TeamCount = size,
                    QualifiedCount = dto.QualifiedPerGroup
                });
            }

            messages.Add(
                $"Creados {sizes.Length} grupos (tamaños: {string.Join(", ", sizes)}). " +
                $"{dto.QualifiedPerGroup} clasificado(s) por grupo.");

            if (dto.AutoGenerateRoundRobinFixtures)
            {
                foreach (var g in groupResults)
                {
                    await _fixtureService.GenerateRoundRobin(g.Id);
                    messages.Add($"Fixture round robin generado en {g.Name}.");
                }
            }
            else
            {
                messages.Add("No se generaron partidos: podés llamar a generate-round-robin por cada grupo cuando quieras.");
            }

            await PersistFormatSetupSnapshotAsync(competitionId, cancellationToken,
                (CompetitionFormatSetupSnapshotKeys.Mode, CompetitionFormatSetupMode.GroupStageRoundRobin.ToString()),
                (CompetitionFormatSetupSnapshotKeys.GroupsMaxPerGroup, dto.MaxTeamsPerGroup.ToString()),
                (CompetitionFormatSetupSnapshotKeys.GroupsQualifiedPerGroup, dto.QualifiedPerGroup.ToString()),
                (CompetitionFormatSetupSnapshotKeys.GroupsShuffle, dto.ShuffleTeams ? "true" : "false"),
                (CompetitionFormatSetupSnapshotKeys.GroupsAutoRoundRobin, dto.AutoGenerateRoundRobinFixtures ? "true" : "false"),
                (CompetitionFormatSetupSnapshotKeys.GroupsPhaseName, dto.GroupPhaseName ?? ""));

            await tx.CommitAsync(cancellationToken);

            return new SetupCompetitionFormatResultDto
            {
                CompetitionId = competitionId,
                Mode = CompetitionFormatSetupMode.GroupStageRoundRobin,
                PhaseId = phaseEntity.Id,
                PhaseName = phaseEntity.Name,
                Groups = groupResults,
                Messages = messages
            };
        }

        private async Task PersistFormatSetupSnapshotAsync(
            Guid competitionId,
            CancellationToken cancellationToken,
            params (string Key, string Value)[] rows)
        {
            foreach (var (key, value) in rows)
            {
                var row = await _context.CompetitionRules
                    .FirstOrDefaultAsync(r => r.CompetitionId == competitionId && r.RuleKey == key, cancellationToken);
                if (row == null)
                {
                    _context.CompetitionRules.Add(new CompetitionRule
                    {
                        CompetitionId = competitionId,
                        RuleKey = key,
                        RuleValue = value
                    });
                }
                else
                    row.RuleValue = value;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
