using Siged.Domain.Entities.Security;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siged.Domain.Entities.Core
{
    public class Organizacion
    {
        public int Id { get; set; }

        // --- DATOS PRINCIPALES ---
        public required string Nombre { get; set; } // Ej: "Facultad de Ingeniería en Informática y Sistemas"
        public required string Abreviatura { get; set; } // Ej: "FIIS" (Vital para tablas de posiciones)
        public string Tipo { get; set; } = "Facultad"; // Ej: "Universidad", "Facultad", "Escuela"

        // --- IDENTIDAD VISUAL E HISTORIA ---
        [MaxLength(2000)]
        public string? Descripcion { get; set; }
        public string? Lema { get; set; }
        public DateTime? FechaCreacion { get; set; }

        // --- MULTIMEDIA (Para el futuro portal público) ---
        [Column(TypeName = "text")]
        public string? LogoUrl { get; set; }
        [Column(TypeName = "text")]
        public string? PortadaUrl { get; set; }

        // 💡 EL TOQUE MAESTRO PARA TU TESIS DEPORTIVA
        public string? ColorRepresentativo { get; set; } // Ej: "#004080" (Azul FIIS). ¡Servirá para pintar los gráficos, las llaves de torneos y los uniformes en la UI!

        // --- ESTADO ---
        public bool EstaActivo { get; set; } = true;

        // --- RELACIONES (Entity Framework) ---
        // Una organización (Facultad) tiene muchos Usuarios (Estudiantes, Encargados)
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}