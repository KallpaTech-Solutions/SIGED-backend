using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Security;
using Siged.Application.Interfaces.Security;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence; // <--- 1. ASEGÚRATE DE TENER ESTE USING

namespace Siged.Api.Controllers.Security
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context; // <--- 2. DECLARA EL CONTEXTO

        // 3. INYECTA EL CONTEXTO EN EL CONSTRUCTOR
        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request.Username, request.Password);

                if (result == null)
                    return Unauthorized(new { message = "Credenciales incorrectas." });

                return Ok(new
                {
                    token = result.Token,
                    username = result.Username,
                    rol = result.Rol,
                    nombreCompleto = result.NombreCompleto,
                    requiereCambioPassword = result.RequiereCambioPassword,
                    message = result.RequiereCambioPassword
                        ? "Debe actualizar su contraseña por seguridad."
                        : "Bienvenido al sistema."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // 1. Extraer el token y el ID del usuario
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);

            // 2. Obtener fecha de expiración del token
            var expClaim = User.FindFirst("exp")?.Value;
            if (string.IsNullOrEmpty(expClaim)) return BadRequest();
            var fechaExp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;

            // 3. Insertar en Blacklist y Auditoría
            // Ahora _context ya existe y no dará error
            _context.TokensInvalidados.Add(new TokenInvalidado
            {
                Token = token,
                FechaExpiracion = fechaExp
            });

            _context.AuditoriaLogs.Add(new AuditoriaLog
            {
                UsuarioId = userId,
                Accion = "LOGOUT",
                Detalle = "Cierre de sesión seguro desde el frontend",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Sesión cerrada con éxito." });
        }
    }
}