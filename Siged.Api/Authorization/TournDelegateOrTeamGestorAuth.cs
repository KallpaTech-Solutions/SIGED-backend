namespace Siged.Api.Authorization;

/// <summary>
/// Inscripción a competencia, plantel y lecturas asociadas: delegado de escuela, admin de torneo o gestor explícito de algún equipo.
/// </summary>
public static class TournDelegateOrTeamGestorAuth
{
    public const string PolicyName = "tourn.delegate.or.team.gestor";
}
