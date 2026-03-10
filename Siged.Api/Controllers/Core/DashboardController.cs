using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Siged.Api.Controllers.Admin
{
    /// <summary>
    /// Controlador para proveer métricas y resúmenes al Panel de Control (Dashboard).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Obtiene el resumen de estadísticas del sistema adaptado al rol del usuario autenticado.
        /// </summary>
        /// <remarks>
        /// 💡 **Comportamiento Dinámico:**
        /// - **SuperAdmin / Admin:** Retorna métricas globales (Total de usuarios, facultades, etc.).
        /// - **Encargado:** Retorna métricas específicas de la facultad a la que pertenece.
        /// - **Estudiante:** Retorna un resumen personal (equipos inscritos, faltas, etc.).
        /// </remarks>
        /// <response code="200">Resumen obtenido exitosamente. El esquema del JSON varía según el rol.</response>
        /// <response code="401">El token de sesión es inválido o ha expirado.</response>
        /// <response code="403">El usuario tiene un rol no reconocido por el sistema.</response>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSummary()
        {
            // 1. Identificamos quién está pidiendo la información
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdString, out int userId);

            // 2. Si es SUPERADMIN o ADMIN, ven todo el panorama de la UNAS
            if (rol == "SuperAdmin" || rol == "Admin")
            {
                var totalUsuarios = await _context.Usuarios.CountAsync();
                var totalFacultades = await _context.Organizaciones.CountAsync();
                var usuariosActivos = await _context.Usuarios.CountAsync(u => u.EstaActivo);

                return Ok(new
                {
                    TipoVista = "Global",
                    TotalUsuarios = totalUsuarios,
                    TotalFacultades = totalFacultades,
                    UsuariosActivos = usuariosActivos,
                    TotalTorneos = 0, // Por implementar
                    UltimosUsuarios = await _context.Usuarios
                        .OrderByDescending(u => u.Id)
                        .Take(5)
                        .Select(u => u.Username)
                        .ToListAsync()
                });
            }

            // 3. Si es ENCARGADO, solo ve lo de su Facultad
            if (rol == "Encargado")
            {
                // Buscamos a qué organización pertenece el encargado
                var usuario = await _context.Usuarios.FindAsync(userId);

                return Ok(new
                {
                    TipoVista = "Facultad",
                    Mensaje = "Bienvenido a la gestión de tu Facultad",
                    TorneosActivos = 0, // Torneos de su facultad (por implementar)
                    EquiposInscritos = 0 // Equipos de su facultad (por implementar)
                });
            }

            // 4. Si es ESTUDIANTE, ve su perfil y sus equipos
            if (rol == "Estudiante")
            {
                return Ok(new
                {
                    TipoVista = "Personal",
                    Mensaje = "Bienvenido a tu panel de estudiante",
                    MisEquipos = 0,
                    MisFaltas = 0
                });
            }

            // 5. Si el rol no hace match con nada, acceso denegado
            return Forbid();
        }
    }
}