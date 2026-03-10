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
        // 1. Extraer los IDs técnicos (strings) del Rol y los Especiales
        // ✅ Cambio: Usamos las colecciones directas y la propiedad IdPermiso
        var permisosRol = usuario.Rol.Permisos.Select(p => p.IdPermiso);
        var permisosEspeciales = usuario.PermisosEspeciales.Select(p => p.IdPermiso);

        // Union une ambas listas y elimina automáticamente los duplicados
        var todosLosPermisos = permisosRol.Union(permisosEspeciales);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new(ClaimTypes.Role, usuario.Rol.Nombre), // Mantenemos el Rol por compatibilidad general
            new("RequiereCambio", usuario.RequiereCambioPassword.ToString().ToLower())
        };

        // 2. Agregar cada permiso a la mochila del Token (Claim "permission")
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