using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Siged.Domain.Entities.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Siged.Infrastructure.Security;

public class JwtProvider
{
    private readonly IConfiguration _config;

    public JwtProvider(IConfiguration config) => _config = config;

    public string Generate(Usuario usuario)
    {
        var permisosRol = usuario.Rol.Permisos.Select(p => p.IdPermiso);
        var permisosEspeciales = usuario.PermisosEspeciales.Select(p => p.IdPermiso);
        var todosLosPermisos = permisosRol.Union(permisosEspeciales);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new(ClaimTypes.Role, usuario.Rol.Nombre), // Mantenemos el Rol por compatibilidad general
            new("RequiereCambio", usuario.RequiereCambioPassword.ToString().ToLower())
        };
        foreach (var permiso in todosLosPermisos)
        {
            claims.Add(new Claim("permission", permiso));
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["JwtSettings:ExpiryMinutes"]!)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}