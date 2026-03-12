using System.ComponentModel.DataAnnotations;

namespace Siged.Domain.Entities.Core.Notice
{
    public class NewsMedia
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Url { get; set; } = string.Empty;

        [Required] // "image" o "video"
        public string MediaType { get; set; } = "image";

        // Clave foránea hacia News
        public Guid NewsId { get; set; }
        public virtual News News { get; set; } = null!;
    }
}