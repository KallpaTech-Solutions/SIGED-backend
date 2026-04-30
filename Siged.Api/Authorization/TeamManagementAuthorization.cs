using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Authorization;

/// <summary>
/// Permisos de edición por equipo: gestores explícitos o política legacy (toda la escuela) si aún no hay filas en TeamGestores.
/// </summary>
public static class TeamManagementAuthorization
{
    public const int MaxDelegadosPorEquipo = 2;

    public static int? GetUsuarioIdFromClaims(ClaimsPrincipal user)
    {
        var s = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }

    public static async Task<bool> TeamHasExplicitGestoresAsync(ApplicationDbContext db, Guid teamId,
        CancellationToken ct = default) =>
        await db.TeamGestores.AsNoTracking().AnyAsync(g => g.TeamId == teamId, ct);

    public static async Task<bool> CanManageTeamAsync(ClaimsPrincipal user, ApplicationDbContext db, Guid teamId,
        CancellationToken ct = default)
    {
        if (TournDelegateAuth.IsTournamentAdmin(user))
            return true;

        var uid = GetUsuarioIdFromClaims(user);
        if (uid == null) return false;

        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return false;

        if (await TeamHasExplicitGestoresAsync(db, teamId, ct))
            return await db.TeamGestores.AsNoTracking()
                .AnyAsync(g => g.TeamId == teamId && g.UsuarioId == uid.Value, ct);

        var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(user, db);
        return orgId != null && orgId.Value == team.OrganizacionId;
    }

    /// <summary>
    /// Permiso estricto para planillas de partido: admin de torneo o gestor explícito del equipo.
    /// Si el equipo aún no tiene gestores explícitos, se permite al usuario creador del equipo.
    /// </summary>
    public static async Task<bool> CanSubmitMatchLineupAsync(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        Guid teamId,
        CancellationToken ct = default)
    {
        var canOperateLineups = user.Claims.Any(c =>
            c.Value == Permissions.TournLineupManage || c.Value == Permissions.TournManage);
        if (canOperateLineups)
            return true;

        if (TournDelegateAuth.IsTournamentAdmin(user))
            return true;

        var uid = GetUsuarioIdFromClaims(user);
        if (uid == null) return false;

        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return false;

        if (await TeamHasExplicitGestoresAsync(db, teamId, ct))
        {
            return await db.TeamGestores.AsNoTracking()
                .AnyAsync(g => g.TeamId == teamId && g.UsuarioId == uid.Value, ct);
        }

        if (team.CreatedByUsuarioId.HasValue)
            return team.CreatedByUsuarioId.Value == uid.Value;

        // Compatibilidad legacy: equipos históricos sin creador registrado.
        // Solo aplica cuando tampoco hay gestores explícitos.
        var orgId = await TournDelegateAuth.GetOrganizacionIdAsync(user, db);
        return orgId != null && orgId.Value == team.OrganizacionId;
    }

    public static async Task<bool> IsPrincipalGestorAsync(ApplicationDbContext db, Guid teamId, int usuarioId,
        CancellationToken ct = default) =>
        await db.TeamGestores.AsNoTracking().AnyAsync(
            g => g.TeamId == teamId && g.UsuarioId == usuarioId && g.Kind == TeamGestorKind.Principal,
            ct);
}
