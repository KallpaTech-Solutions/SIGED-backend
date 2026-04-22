using System.Linq;
using System.Security.Claims;
using Siged.Domain.Entities.Security;

namespace Siged.Api.Authorization;

/// <summary>
/// Armado inicial de formato (grupos / eliminatoria): gestores OTI o quienes configuran fases en cancha.
/// </summary>
public static class TournFormatSetupAuth
{
    public const string PolicyName = "tourn.format.setup";

    public static bool CanSetupFormat(ClaimsPrincipal user)
    {
        var p = user.FindAll("permission").Select(c => c.Value).ToHashSet();
        return p.Contains(Permissions.TournManage)
            || p.Contains(Permissions.TournConfig)
            || p.Contains(Permissions.TournFixture);
    }
}
