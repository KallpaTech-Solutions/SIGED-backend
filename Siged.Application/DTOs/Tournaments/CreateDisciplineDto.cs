using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments
{
    public class CreateDisciplineDto
    {
        [Required(ErrorMessage = "El nombre de la disciplina es obligatorio.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // Para que el administrador suba un icono personalizado (PNG/SVG)
        public IFormFile? IconFile { get; set; }

        public string? Description { get; set; }
    }
}
