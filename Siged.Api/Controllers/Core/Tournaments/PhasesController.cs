using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PhasesController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Create(CreatePhaseDto dto)
        {
            var phase = new Phase
            {
                CompetitionId = dto.CompetitionId,
                Name = dto.Name,
                Type = dto.Type,
                Order = dto.Sequence
            };

            _context.Phases.Add(phase);
            await _context.SaveChangesAsync();
            return Ok(phase);
        }

        [HttpGet("competition/{competitionId}")]
        public async Task<IActionResult> GetByCompetition(Guid competitionId)
        {
            var phases = await _context.Phases
                .Include(p => p.Groups) // Traer los grupos de la fase
                .Where(p => p.CompetitionId == competitionId)
                .OrderBy(p => p.Order)
                .ToListAsync();

            return Ok(phases);
        }
    }
}
