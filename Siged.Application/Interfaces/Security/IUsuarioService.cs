using Siged.Domain.Entities.Security;

namespace Siged.Application.Interfaces.Security
{
    public interface IUsuarioService
    {
        // Lógica de jerarquía tipo "WhatsApp"
        bool PuedeGestionar(Usuario ejecutor, Usuario objetivo, string accion);

        // ✅ CAMBIO: Ahora recibe un 'string' para coincidir con la implementación
        Task AsignarPermisoDirecto(int usuarioId, string permisoId);
    }
}