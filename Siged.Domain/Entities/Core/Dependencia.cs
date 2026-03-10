namespace Siged.Domain.Entities.Core
{
    public class Dependencia
    {
        public int Id { get; set; }
        public required string Nombre { get; set; } // Ejemplo: "OTI"
        public string? Siglas { get; set; }       // Ejemplo: "Oficina de Tecnología"

        // Relación: Una dependencia tiene muchos administradores
        public ICollection<Administrador> Administradores { get; set; } = new List<Administrador>();
    }
}