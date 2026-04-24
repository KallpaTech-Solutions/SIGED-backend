using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Domain.Entities.Security;

public class Usuario
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public bool EstaActivo { get; set; } = true;

    // Relación con el Rol (RBAC)
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    // ✅ Permisos asignados a dedo, ignorando el rol
    public virtual ICollection<Permiso> PermisosEspeciales { get; set; } = new List<Permiso>();

    // Vinculación con la Persona (Estudiante o Encargado de la UNAS)
    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;
    public int? OrganizacionId { get; set; } // Opcional, porque el SuperAdmin no pertenece a una facultad
    public Organizacion? Organizacion { get; set; } // ✅ Propiedad de navegación

    // JERARQUÍA: Auto-relación para saber quién creó a este usuario
    public int? CreadoPorUsuarioId { get; set; }
    public Usuario? Creador { get; set; }
    public bool RequiereCambioPassword { get; set; } = true; // Por defecto true al crear
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TeamGestor> EquiposGestionados { get; set; } = new List<TeamGestor>();
}