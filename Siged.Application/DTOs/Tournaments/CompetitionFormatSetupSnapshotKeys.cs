namespace Siged.Application.DTOs.Tournaments
{
    /// <summary>Claves en <see cref="CompetitionRule"/> para recordar lo elegido al armar el formato inicial.</summary>
    public static class CompetitionFormatSetupSnapshotKeys
    {
        public const string Mode = "FORMAT_SETUP_MODE";
        public const string GroupsMaxPerGroup = "FORMAT_SETUP_GROUPS_MAX_PER_GROUP";
        public const string GroupsQualifiedPerGroup = "FORMAT_SETUP_GROUPS_QUALIFIED_PER_GROUP";
        public const string GroupsShuffle = "FORMAT_SETUP_GROUPS_SHUFFLE";
        public const string GroupsAutoRoundRobin = "FORMAT_SETUP_GROUPS_AUTO_RR";
        public const string GroupsPhaseName = "FORMAT_SETUP_GROUPS_PHASE_NAME";
        public const string KnockoutPhaseName = "FORMAT_SETUP_KNOCKOUT_PHASE_NAME";
        public const string KnockoutRandom = "FORMAT_SETUP_KNOCKOUT_RANDOM";
    }
}
