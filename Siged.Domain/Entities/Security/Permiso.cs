using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Security;

public class Permiso
{
    [Key]
    public string IdPermiso { get; set; } = null! ;
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = null!;

    
    // Le dice a EF Core "Un permiso lo pueden tener muchos Roles"
    public ICollection<Rol> Roles { get; set; } = new List<Rol>();

    // Le dice a EF Core "Un permiso especial lo pueden tener muchos Usuarios"
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}