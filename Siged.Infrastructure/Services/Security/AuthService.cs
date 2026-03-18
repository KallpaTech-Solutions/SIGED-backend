using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Security;
using Siged.Application.Interfaces.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Security;

namespace Siged.Infrastructure.Services.Security
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly JwtProvider _jwtProvider;

        public AuthService(ApplicationDbContext context, IPasswordHasher passwordHasher, JwtProvider jwtProvider)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
        }

        /// <summary>
        /// Realiza la autenticación del usuario, verificando credenciales y estado de cuenta.
        /// Carga la matriz completa de permisos (Rol + Especiales) para la generación del Token.
        /// </summary>
        public async Task<LoginResponseDto?> LoginAsync(string username, string password)
        {
            // 1. Buscamos solo los campos necesarios (Proyección)
            var datosUsuario = await _context.Usuarios
                .AsNoTracking() // 👈 No necesitamos rastrear cambios para un login
                .Where(u => u.Username == username)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.PasswordHash,
                    u.EstaActivo,
                    u.RequiereCambioPassword,
                    Nombre = u.Persona.Nombres,
                    Apellido = u.Persona.Apellidos,
                    RolNombre = u.Rol.Nombre,
                    // Solo los nombres de los permisos (no el objeto completo)
                    Permisos = u.Rol.Permisos.Select(p => p.IdPermiso).ToList(),
                    PermisosEspeciales = u.PermisosEspeciales.Select(p => p.IdPermiso).ToList()
                })
                .FirstOrDefaultAsync();

            if (datosUsuario == null) return null;

            // 2. Validaciones de estado y password
            if (!datosUsuario.EstaActivo)
                throw new UnauthorizedAccessException("Tu cuenta ha sido desactivada.");

            if (!_passwordHasher.Verify(password, datosUsuario.PasswordHash))
                return null;

            // 3. Generamos el Token
            // Nota: Aquí tendrías que ajustar tu JwtProvider para que reciba estos datos
            // o crear un objeto Usuario temporal solo con lo necesario.
            var todosLosPermisos = datosUsuario.Permisos.Concat(datosUsuario.PermisosEspeciales).Distinct().ToList();

            var token = _jwtProvider.Generate(
                datosUsuario.Id,
                datosUsuario.Username,
                datosUsuario.RolNombre,
                todosLosPermisos,
                datosUsuario.RequiereCambioPassword
            );

            return new LoginResponseDto
            {
                Token = token,
                Username = datosUsuario.Username,
                Rol = datosUsuario.RolNombre,
                NombreCompleto = $"{datosUsuario.Nombre} {datosUsuario.Apellido}".Trim(),
                RequiereCambioPassword = datosUsuario.RequiereCambioPassword
            };
        }
    }
}