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
            // 1. Buscamos al usuario cargando su perfil y TODA su matriz de permisos
            // ✅ Cambio: Usamos las nuevas colecciones directas de Permisos
            var usuario = await _context.Usuarios
                .Include(u => u.Persona)
                .Include(u => u.Rol)
                    .ThenInclude(r => r.Permisos) // Carga permisos heredados del Rol
                .Include(u => u.PermisosEspeciales) // Carga permisos asignados "a dedo"
                .FirstOrDefaultAsync(u => u.Username == username);

            // 2. Validaciones de existencia
            if (usuario == null) return null;

            // 3. Validación de estado (Inactivación lógica)
            if (!usuario.EstaActivo)
            {
                throw new UnauthorizedAccessException("Tu cuenta ha sido desactivada. Por favor, contacta a la OTI.");
            }

            // 4. Verificar contraseña con el Hash de la DB
            bool isPasswordValid = _passwordHasher.Verify(password, usuario.PasswordHash);
            if (!isPasswordValid) return null;

            // 5. Generar el Token JWT
            // El JwtProvider debe estar preparado para leer 'usuario.Rol.Permisos' 
            // y 'usuario.PermisosEspeciales' para meterlos en los Claims.
            var token = _jwtProvider.Generate(usuario);

            // 6. Mapeo a DTO de respuesta para el Frontend
            return new LoginResponseDto
            {
                Token = token,
                Username = usuario.Username,
                Rol = usuario.Rol.Nombre,
                NombreCompleto = $"{usuario.Persona.Nombres} {usuario.Persona.Apellidos}".Trim(),
                RequiereCambioPassword = usuario.RequiereCambioPassword
            };
        }
    }
}