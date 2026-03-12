using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Notice
{
    public class News
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Slug { get; set; } = string.Empty; // Para la URL amigable

        [Required, MaxLength(250)]
        public string Excerpt { get; set; } = string.Empty; // Resumen inicial

        [Required]
        public string Content { get; set; } = string.Empty; // Cuerpo de la noticia

        public string Category { get; set; } = "Institucional";

        public string Tags { get; set; } = string.Empty;

        // Configuración de interacción
        public bool IsFeatured { get; set; } = false;
        public bool AllowComments { get; set; } = true;
        public bool AllowReactions { get; set; } = true;

        public NewsStatus Status { get; set; } = NewsStatus.Draft;

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relación: Una noticia puede tener mucha Media (Fotos/Videos)
        public virtual ICollection<NewsMedia> Media { get; set; } = new List<NewsMedia>();

        public static string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title)) return string.Empty;

            // 1. Minúsculas, quitar espacios al inicio y final
            string str = title.ToLowerInvariant().Trim();

            // 2. Reemplazar caracteres especiales (acentos)
            // (Opcional, pero recomendado para español)

            // 3. Reemplazar todo lo que no sea letra o número por un guion
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");

            // 4. Reemplazar espacios o múltiples guiones por un solo guion
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[\s-]+", "-").Trim('-');

            return str;
        }
    }

}