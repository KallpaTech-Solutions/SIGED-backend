using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Authorization;

public sealed class TournDelegateOrTeamGestorHandler : AuthorizationHandler<TournDelegateOrTeamGestorRequirement>
{
    private readonly ApplicationDbContext _db;

    public TournDelegateOrTeamGestorHandler(ApplicationDbContext db) => _db = db;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TournDelegateOrTeamGestorRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        if (TournDelegateAuth.IsTournamentAdmin(context.User) ||
            context.User.HasClaim("permission", Permissions.TournTeamManage))
        {
            context.Succeed(requirement);
            return;
        }

        var uidStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(uidStr, out var uid))
            return;

        if (await _db.TeamGestores.AsNoTracking().AnyAsync(g => g.UsuarioId == uid))
            context.Succeed(requirement);
    }
}
