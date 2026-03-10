using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Siged.Infrastructure.Services.Security;

// FIJATE AQUÍ: Debe tener la herencia y el tipo de requisito entre < >
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}