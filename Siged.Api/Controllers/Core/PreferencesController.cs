using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Siged.Infrastructure.Persistence;
using Siged.Domain.Entities.Security;
using System.Security.Claims;

namespace Siged.Api.Controllers.Core
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 Bloqueamos todo el controlador: requiere Token
    public class PreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos el contexto para poder hablar con Supabase
        public PreferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene las preferencias actuales del usuario autenticado.
        /// </summary>
        [HttpGet("my-config")]
        public async Task<IActionResult> GetMyPreferences()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var pref = await _context.UserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Si no tiene, devolvemos los valores por defecto
            return Ok(new
            {
                widgetsVisibles = pref?.WidgetsVisibles ?? "usuarios,orgs,torneos,activos,recent,banner_torneos",
                tema = pref?.Tema ?? "light"
            });
        }

        /// <summary>
        /// Guarda o actualiza la lista de widgets que el usuario quiere ver.
        /// </summary>
        [HttpPost("update")]
        public async Task<IActionResult> UpdatePreferences([FromBody] List<string> selectedWidgets)
        {
            try
            {
                // 1. Extraer ID del Token
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                // 2. Normalizar la lista a un solo string
                var widgetsString = string.Join(",", selectedWidgets.Select(s => s.ToLower().Trim()));

                // 3. Buscar si ya existe la configuración
                var pref = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

                if (pref == null)
                {
                    _context.UserPreferences.Add(new UserPreference
                    {
                        UserId = userId,
                        WidgetsVisibles = widgetsString,
                        UltimaActualizacion = DateTime.UtcNow
                    });
                }
                else
                {
                    pref.WidgetsVisibles = widgetsString;
                    pref.UltimaActualizacion = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Dashboard personalizado con éxito", widgets = widgetsString });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar preferencias", details = ex.Message });
            }
        }
    }
}