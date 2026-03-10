using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Domain.Entities.Core
{
    public abstract class Persona
    {
        public int Id { get; set; }
    
        // Al poner 'required', obligas a que se llenen al crear el objeto
        public required string DNI { get; set; }
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public string? FotoPath { get; set; }

        // Con el '?' permites que sean NULL en la base de datos
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
    }
}
