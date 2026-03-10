using Microsoft.EntityFrameworkCore;
using Siged.Application.Interfaces.Security;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Application.Services.Security
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;

        public UsuarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Valida si un usuario tiene jerarquía suficiente para realizar acciones sobre otro.
        /// </summary>
        public bool PuedeGestionar(Usuario ejecutor, Usuario objetivo, string accion)
        {
            // 1. EL PRIMER SUPERADMIN (ID 1) ES INMORTAL.
            // No se puede eliminar ni inactivar al administrador principal del sistema.
            if (objetivo.Id == 1 && (accion == "ELIMINAR" || accion == "INACTIVAR"))
                return false;

            // 2. REGLA PARA SUPERADMINS: Control total sobre el sistema.
            if (ejecutor.Rol.Nombre == "SuperAdmin") return true;

            // 3. REGLA PARA ADMINS:
            if (ejecutor.Rol.Nombre == "Admin")
            {
                // El Admin no puede gestionar a un SuperAdmin.
                if (objetivo.Rol.Nombre == "SuperAdmin") return false;

                // Solo el creador original puede eliminar o inactivar a un usuario.
                if (accion == "ELIMINAR" || accion == "INACTIVAR")
                {
                    return objetivo.CreadoPorUsuarioId == ejecutor.Id;
                }

                // Para otras acciones (ver/editar) se permite si no es SuperAdmin.
                return true;
            }

            return false; // Por defecto, otros roles no gestionan usuarios.
        }

        /// <summary>
        /// Asigna un permiso individual al usuario, sumándose a los que ya tiene por su Rol.
        /// </summary>
        /// <param name="usuarioId">ID numérico del usuario.</param>
        /// <param name="permisoId">ID técnico del permiso (ej: "security.user.view").</param>
        public async Task AsignarPermisoDirecto(int usuarioId, string permisoId)
        {
            // 1. Cargamos el usuario incluyendo su lista actual de permisos especiales
            var usuario = await _context.Usuarios
                .Include(u => u.PermisosEspeciales)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null) return;

            // 2. Verificamos si ya posee el permiso para evitar duplicados en la colección
            if (!usuario.PermisosEspeciales.Any(p => p.IdPermiso == permisoId))
            {
                // 3. Buscamos el objeto Permiso en el catálogo
                var permiso = await _context.Permisos.FindAsync(permisoId);

                if (permiso != null)
                {
                    // 4. Agregamos directamente a la colección. 
                    // EF Core se encargará de insertar en la tabla "UsuariosPermisos" automáticamente.
                    usuario.PermisosEspeciales.Add(permiso);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}