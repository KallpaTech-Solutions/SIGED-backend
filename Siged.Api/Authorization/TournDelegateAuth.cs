using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Authorization;

/// <summary>
/// Helpers para delegados (tourn.team.manage) vs administración de torneo (tourn.manage).
/// </summary>
public static class TournDelegateAuth
{
    public const string PolicyName = "tourn.delegate.actions";

    public static bool IsTournamentAdmin(ClaimsPrincipal user) =>
        user.HasClaim("permission", Permissions.TournManage);

    public static async Task<int?> GetOrganizacionIdAsync(ClaimsPrincipal user, ApplicationDbContext db)
    {
        var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var uid)) return null;
        var u = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == uid);
        return u?.OrganizacionId;
    }
}
