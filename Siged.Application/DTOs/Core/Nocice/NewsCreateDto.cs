using Siged.Domain.Entities.Core.Notice;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Core.Nocice
{
    public class NewsCreateDto
    {
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Excerpt { get; set; } = string.Empty;
        [Required] public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = "Institucional";
        public string Tags { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool AllowComments { get; set; } = true;
        public bool AllowReactions { get; set; } = true;
        public NewsStatus Status { get; set; } = NewsStatus.Draft;
        public List<string>? MediaUrls { get; set; }
    }

    public class NewsUpdateDto : NewsCreateDto { }
}
