using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Infrastructure.Persistence; // Asegúrate de tener este using
using Microsoft.EntityFrameworkCore;

namespace Siged.Api.Controllers.Security
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos el contexto para probar la conexión
        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // 📡 Intentamos una operación ultra ligera en la DB
                // CanConnectAsync solo verifica si el "caño" hacia Supabase está abierto
                bool canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return StatusCode(503, new { status = "Unhealthy", database = "Disconnected" });
                }

                return Ok(new
                {
                    status = "Healthy",
                    database = "Connected",
                    serverTime = DateTime.UtcNow,
                    environment = "Production (Render)"
                });
            }
            catch (Exception ex)
            {
                // Si algo explota, devolvemos el error para saber qué pasó
                return StatusCode(500, new { status = "Error", message = ex.Message });
            }
        }
    }
}