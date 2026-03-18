using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Siged.Application.DTOs.Core;

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

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] string? blocks)
        {
            try
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

                // 1. Obtener preferencias de la DB si no se pasan por URL
                if (string.IsNullOrEmpty(blocks))
                {
                    var pref = await _context.UserPreferences.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    blocks = pref?.WidgetsVisibles ?? "all";
                }

                var requested = blocks.ToLower().Split(',');
                var isAll = requested.Contains("all");
                var result = new Dictionary<string, object>();

                // 2. Filtrado por Permisos + Preferencias
                // Bloque Usuarios
                if (User.HasClaim("permission", "security.user.view"))
                {
                    if (isAll || requested.Contains("usuarios"))
                        result["totalUsuarios"] = await _context.Usuarios.CountAsync();

                    if (isAll || requested.Contains("activos"))
                        result["usuariosActivos"] = await _context.Usuarios.CountAsync(u => u.EstaActivo);
                }

                // Bloque Organizaciones
                if (User.HasClaim("permission", "core.org.view"))
                {
                    if (isAll || requested.Contains("orgs") || requested.Contains("organizaciones"))
                        result["totalFacultades"] = await _context.Organizaciones.CountAsync();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}