using System.ComponentModel.DataAnnotations;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;

namespace Siged.Domain.Entities.Core.Tournaments;

/// <summary>
/// Usuario autorizado a gestionar un equipo (plantel, inscripción según políticas del controlador).
/// </summary>
public class TeamGestor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public int UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;

    public TeamGestorKind Kind { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Usuario que otorgó el rol (p. ej. delegado principal o SuperAdmin).</summary>
    public int? AssignedByUsuarioId { get; set; }
}
