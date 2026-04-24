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
                .Where(p => p.CompetitionId == competitionId)
                .OrderBy(p => p.Order)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Type,
                    p.Order,
                    p.IsDirectElimination,
                    GroupsCount = p.Groups.Count,
                    // Si es Knockout, nos interesará saber cuántos equipos hay
                    TotalTeams = p.Groups.Sum(g => g.GroupTeams.Count)
                })
                .ToListAsync();

            return Ok(phases);
        }
    }
}
