using Microsoft.AspNetCore.Authorization; 

namespace Siged.Infrastructure.Services.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}