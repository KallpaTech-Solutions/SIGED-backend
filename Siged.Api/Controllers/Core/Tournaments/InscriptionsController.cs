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

            var maxTeamsPerOrganization = competition.MaxTeamsPerOrganization;
            if (maxTeamsPerOrganization > 0)
            {
                var alreadyInscribedForSchool = await _context.CompetitionTeams
                    .AsNoTracking()
                    .Include(ct => ct.Team)
                    .CountAsync(ct =>
                        ct.CompetitionId == dto.CompetitionId &&
                        ct.Team.OrganizacionId == team.OrganizacionId);

                if (alreadyInscribedForSchool >= maxTeamsPerOrganization)
                {
                    return BadRequest(new
                    {
                        message = $"Esta competencia permite máximo {maxTeamsPerOrganization} equipo(s) por escuela. Tu escuela ya tiene {alreadyInscribedForSchool} inscrito(s)."
                    });
                }
            }

            // 2. Crear la inscripción
            var inscription = new CompetitionTeam
            {
                CompetitionId = dto.CompetitionId,
                TeamId = dto.TeamId,
                FechaInscripcion = DateTime.Now
            };

            _context.CompetitionTeams.Add(inscription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscripción exitosa.", competition.MaxTeamsPerOrganization });
        }

        [HttpDelete("{competitionId:guid}/teams/{teamId:guid}")]
        [Authorize(Policy = TournDelegateOrTeamGestorAuth.PolicyName)]
        public async Task<IActionResult> Delete(Guid competitionId, Guid teamId)
        {
            var competition = await _context.Competitions
                .AsNoTracking()
                .Include(c => c.Tournament)
                .FirstOrDefaultAsync(c => c.Id == competitionId);

            if (competition == null) return NotFound("Competencia no encontrada.");

            if (competition.Tournament.Status != TournamentStatus.InscripcionesAbiertas)
                return BadRequest("Solo se puede quitar una inscripción mientras el torneo está en inscripciones abiertas.");

            var inscription = await _context.CompetitionTeams
                .Include(ct => ct.Team)
                .FirstOrDefaultAsync(ct => ct.CompetitionId == competitionId && ct.TeamId == teamId);

            if (inscription == null)
                return NotFound("No se encontró la inscripción del equipo en esta competencia.");

            if (!await TeamManagementAuthorization.CanManageTeamAsync(User, _context, teamId))
                return Forbid();

            var usedInGroups = await _context.GroupTeams
                .AsNoTracking()
                .AnyAsync(gt => gt.TeamId == teamId && gt.Group.Phase.CompetitionId == competitionId);
            if (usedInGroups)
                return BadRequest("No se puede quitar la inscripción porque el equipo ya fue asignado a grupos o fixture.");

            var usedInMatches = await _context.Matches
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Phase.CompetitionId == competitionId &&
                    (m.LocalTeamId == teamId || m.VisitorTeamId == teamId));
            if (usedInMatches)
                return BadRequest("No se puede quitar la inscripción porque el equipo ya tiene partidos generados.");

            var usedInLineups = await _context.MatchLineups
                .AsNoTracking()
                .AnyAsync(l => l.TeamId == teamId && l.Match.Phase.CompetitionId == competitionId);
            if (usedInLineups)
                return BadRequest("No se puede quitar la inscripción porque el equipo ya tiene planillas de partido.");

            _context.CompetitionTeams.Remove(inscription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscripción eliminada." });
        }
    }
}
