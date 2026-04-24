using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Api.Authorization;
using Siged.Application.DTOs.Tournaments.Team;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
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
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> Inscribe([FromBody] InscribeTeamDto dto)
        {
            var competition = await _context.Competitions
                .AsNoTracking()
                .Include(c => c.Tournament)
                .FirstOrDefaultAsync(c => c.Id == dto.CompetitionId);

            if (competition == null) return NotFound("Competencia no encontrada.");

            if (competition.Tournament.Status != TournamentStatus.InscripcionesAbiertas)
                return BadRequest(
                    "Las inscripciones solo están habilitadas cuando el torneo está en estado «Inscripciones abiertas».");

            var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == dto.TeamId);
            if (team == null) return BadRequest("Equipo no encontrado.");

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, team.Id))
                return Forbid();

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
