using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Application.DTOs.Tournaments.Group
{
    public class SyncGroupTeamsDto
    {
        public List<Guid> TeamIds { get; set; } = new();
    }
}
