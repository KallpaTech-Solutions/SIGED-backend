using Microsoft.AspNetCore.Http;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using System.ComponentModel.DataAnnotations;

namespace Siged.Application.DTOs.Tournaments.Discipline
{
    public class CreateDisciplineDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        // 🚀 CRUCIAL: Esto es lo que faltaba para conectar con SportRulesTemplates
        [Required(ErrorMessage = "La plantilla es obligatoria.")]
        public string TemplateKey { get; set; } = string.Empty; // Ej: "FIFA_FOOTBALL", "FIFA_FUTSAL"

        public IFormFile? IconFile { get; set; }

        // Lo dejamos opcional por si el usuario quiere forzar un tipo diferente
        public ScoringType? ScoringType { get; set; }

        public string? Description { get; set; }
    }
}