using Microsoft.AspNetCore.Mvc;
using Siged.Application.Interfaces.Security;
using Siged.Application.DTOs.Security;

namespace Siged.Api.Controllers.Security
{
    /// <summary>
    /// Controlador encargado de la autenticación y emisión de tokens de acceso.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) => _authService = authService;

        /// <summary>
        /// Autentica las credenciales de un usuario y genera un Token JWT.
        /// </summary>
        /// <remarks>
        /// Si es el primer ingreso del usuario (RequiereCambioPassword = true), 
        /// el sistema retornará un flag para que React obligue al cambio de clave.
        /// </remarks>
        /// <param name="request">Credenciales (Username y Password).</param>
        /// <response code="200">Retorna el token y datos básicos del perfil.</response>
        /// <response code="401">Credenciales inválidas.</response>
        /// <response code="403">Cuenta desactivada por el administrador.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request.Username, request.Password);

                if (result == null)
                    return Unauthorized(new { message = "Credenciales incorrectas." });

                // ✅ ESTANDARIZACIÓN: Siempre devolvemos el mismo objeto
                return Ok(new
                {
                    token = result.Token,
                    username = result.Username,
                    rol = result.Rol,
                    nombreCompleto = result.NombreCompleto,
                    // 💡 Usamos el mismo nombre que tienes en tu base de datos y DTOs
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
    }
}