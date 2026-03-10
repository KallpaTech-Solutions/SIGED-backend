using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Security
{
    public class RoleResponseDto
    {
        public int Id { get; set; } // 👈 El ID es vital para que React sepa a quién editar
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public int Nivel { get; set; }
        public int UsuariosAsociados { get; set; } // 👈 Útil para saber si se puede borrar
    }
}
