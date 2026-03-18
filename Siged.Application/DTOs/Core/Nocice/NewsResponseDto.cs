using Siged.Domain.Entities.Core.Notice;

namespace Siged.Application.DTOs.Core.Nocice
{
    public class NewsResponseDto
    {
        public Guid Id { get; set; } // El ID generado por la DB
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NoticiaCategoria Category { get; set; } 
        public string Tags { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool AllowComments { get; set; }
        public bool AllowReactions { get; set; }
        public NewsStatus Status { get; set; }

        // Campos de lectura que el Front necesita mostrar
        public DateTime CreatedAt { get; set; }
        public int ViewCount { get; set; } // Para mostrar "Visto por 150 personas"
        public string? Slug { get; set; } // Útil para URLs amigables en el Front

        // 🚀 La lista de URLs limpia para romper el ciclo infinito
        public List<string> MediaUrls { get; set; } = new List<string>();
    }
}