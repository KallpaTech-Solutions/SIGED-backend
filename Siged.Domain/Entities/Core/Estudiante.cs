using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core
{
    public class Estudiante : Persona
    {
        public required string CodigoEstudiante { get; set; }  // Ej: 0020180314
        public bool EstaMatriculado { get; set; } // Para validar si puede jugar
        [JsonIgnore]
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public bool ValidarMatricula()
        {
            // Tu lógica para el botón "verde/rojo" que planeamos
            return EstaMatriculado;
        }
    }
}
