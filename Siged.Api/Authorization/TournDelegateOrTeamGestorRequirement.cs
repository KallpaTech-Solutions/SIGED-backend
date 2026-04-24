using Microsoft.AspNetCore.Authorization;

namespace Siged.Api.Authorization;

/// <summary>
/// Marca la política: delegado de escuela / admin de torneo, o al menos un registro en TeamGestores.
/// </summary>
public sealed class TournDelegateOrTeamGestorRequirement : IAuthorizationRequirement;
