using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Discipline
{
    /// <summary>
    /// Actualización de disciplina (nombre e icono). La plantilla y el tipo de puntaje
    /// se definen al crear; no se recalculan reglas desde plantilla en el PUT.
    /// </summary>
    public class UpdateDisciplineDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public IFormFile? IconFile { get; set; }
    }
}
