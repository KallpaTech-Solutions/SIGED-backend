using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siged.Domain.Entities.Security
{
    public class UserPreference
    {
        [Key]
        public int UserId { get; set; }

        // Guardamos los bloques como un string separado por comas (ej: "usuarios,orgs")
        [Required]
        public string WidgetsVisibles { get; set; } = "usuarios,orgs,torneos,activos,recent,banner_torneos";

        public string Tema { get; set; } = "light";

        public DateTime UltimaActualizacion { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación
        [ForeignKey("UserId")]
        public virtual Usuario Usuario { get; set; } = null!;
    }
}