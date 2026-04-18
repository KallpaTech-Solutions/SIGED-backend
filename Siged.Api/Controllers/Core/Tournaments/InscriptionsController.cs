using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Application.DTOs.Tournaments.Team;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InscriptionsController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Inscribe([FromBody] InscribeTeamDto dto)
        {
            // 1. Validar si ya existe la inscripción
            var exists = await _context.CompetitionTeams.AnyAsync(ct => ct.CompetitionId == dto.CompetitionId && ct.TeamId == dto.TeamId);

            if (exists) return BadRequest("Este equipo ya está inscrito en la competición.");

            // 2. Crear la inscripción
            var inscription = new CompetitionTeam
            {
                CompetitionId = dto.CompetitionId,
                TeamId = dto.TeamId,
                FechaInscripcion = DateTime.Now
            };

            _context.CompetitionTeams.Add(inscription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscripción exitosa." });
        }
    }
}
