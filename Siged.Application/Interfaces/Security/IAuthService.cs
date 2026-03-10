using Siged.Application.DTOs.Security;

namespace Siged.Application.Interfaces.Security
{
    public interface IAuthService
    {
        // Devuelve el token JWT si las credenciales son válidas
        Task<LoginResponseDto?> LoginAsync(string username, string password);
    }
}
