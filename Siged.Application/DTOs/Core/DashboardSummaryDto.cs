using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Core
{
    public class DashboardSummaryDto
    {
        public string TipoVista { get; set; } = string.Empty;
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<string>? UltimosUsuarios { get; set; }
        public string? Mensaje { get; set; }
    }
}
