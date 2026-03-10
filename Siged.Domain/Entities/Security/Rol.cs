namespace Siged.Domain.Entities.Security;

public class Rol
{
    public int Id { get; set; }

    public required string Nombre { get; set; } // Ejemplo: "Admin"

    // Agregamos la descripción para documentar el propósito del rol
    public string? Descripcion { get; set; } // Ejemplo: "Gestiona eventos y usuarios de la OTI"
    public int Nivel { get; set; } = 1; // Por defecto el más bajo

    // El Rol tiene una lista directa de permisos
    public virtual ICollection<Permiso> Permisos { get; set; } = new List<Permiso>();

}