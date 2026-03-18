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

    // --- NUEVO MÉTODO: Recibe datos optimizados ---
    public virtual string Generate(int id, string username, string rol, List<string> permissions, bool requiereCambio = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, rol),
            new("RequiereCambio", requiereCambio.ToString().ToLower())
        };

        foreach (var permiso in permissions)
        {
            claims.Add(new Claim("permission", permiso));
        }

        return CreateToken(claims);
    }

    // --- MÉTODO ANTIGUO: Mantenido por compatibilidad (opcional) ---
    public virtual string Generate(Usuario usuario)
    {
        var permisosRol = usuario.Rol.Permisos.Select(p => p.IdPermiso);
        var permisosEspeciales = usuario.PermisosEspeciales.Select(p => p.IdPermiso);
        var todosLosPermisos = permisosRol.Union(permisosEspeciales).ToList();

        return Generate(usuario.Id, usuario.Username, usuario.Rol.Nombre, todosLosPermisos, usuario.RequiereCambioPassword);
    }

    // --- LÓGICA PRIVADA: Para no repetir código ---
    private string CreateToken(List<Claim> claims)
    {
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