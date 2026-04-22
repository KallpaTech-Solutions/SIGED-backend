using System;

namespace Siged.Application.DTOs.Tournaments
{
    public class SetupCompetitionFormatGroupResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TeamCount { get; set; }
        public int QualifiedCount { get; set; }
    }

    public class SetupCompetitionFormatResultDto
    {
        public Guid CompetitionId { get; set; }
        public CompetitionFormatSetupMode Mode { get; set; }
        public Guid PhaseId { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public IReadOnlyList<SetupCompetitionFormatGroupResultDto> Groups { get; set; } = Array.Empty<SetupCompetitionFormatGroupResultDto>();
        public IReadOnlyList<string> Messages { get; set; } = Array.Empty<string>();
    }
}
