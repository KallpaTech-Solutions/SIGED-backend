using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Services;
using Siged.Application.DTOs.Tournaments.Playoff;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FixturesController : ControllerBase
    {
        private readonly FixtureService _fixtureService; // 👈 Asegúrate que el nombre coincida con tu Service
        private readonly ApplicationDbContext _context;
        private readonly PlayoffService _playoffService;
        private readonly BracketService _bracketService;
        private readonly TournamentVitrinaBroadcastService _vitrina;

        public FixturesController(
            FixtureService fixtureService,
            ApplicationDbContext context,
            PlayoffService playoffService,
            BracketService bracketService,
            TournamentVitrinaBroadcastService vitrina)
        {
            _fixtureService = fixtureService;
            _context = context;
            _playoffService = playoffService;
            _bracketService = bracketService;
            _vitrina = vitrina;
        }

        /// <summary>
        /// Genera un fixture de todos contra todos (Round Robin) para un grupo específico utilizando el algoritmo de Berger.
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        [HttpPost("generate-round-robin/{groupId}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Generate(Guid groupId)
        {
            // 1. Validar si el grupo existe y tiene equipos (No queremos fixture de 0 equipos)
            var group = await _context.Groups
                .Include(g => g.GroupTeams)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound("El grupo no existe.");

            if (group.GroupTeams.Count < 2)
                return BadRequest("Se necesitan al menos 2 equipos para generar un fixture.");

            // 2. Validar si ya existen jornadas para no duplicar
            var exists = await _context.Journals.AnyAsync(j => j.GroupId == groupId);
            if (exists) return BadRequest("El fixture para este grupo ya ha sido generado.");

            // 3. Ejecutar algoritmo Berger
            await _fixtureService.GenerateRoundRobin(groupId);

            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(new { message = "Fixture generado exitosamente con Algoritmo Berger." });
        }

        /// <summary>
        /// Genera las llaves de una fase de eliminación directa (playoffs) a partir de los resultados de una fase de grupos.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("generate-playoffs")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> GeneratePlayoffs([FromBody] GeneratePlayoffDto dto)
        {
            try
            {
                await _playoffService.GenerateKnockoutFromGroups(dto);
                await _vitrina.NotifyLandingRefreshAsync();
                return Ok(new { message = $"Llaves de {dto.NewPhaseName} generadas exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest($"No se pudieron generar las llaves: {ex.Message}");
            }
        }
        /// <summary>
        /// Promueve a los ganadores de una fase de eliminación directa a la siguiente fase, generando los cruces correspondientes.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("promote-winners")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Promote([FromBody] PromoteWinnersDto dto)
        {
            await _playoffService.PromoteWinnersToNextPhase(dto);
            await _vitrina.NotifyLandingRefreshAsync();
            return Ok(new { message = $"Se han generado los cruces para {dto.NextPhaseName}." });
        }

        [HttpGet("phase/{phaseId}/bracket")]
        [AllowAnonymous] // 👈 Importante: Las llaves deben ser públicas
        public async Task<IActionResult> GetBracket(Guid phaseId)
        {
            var result = await _bracketService.GetBracketByPhaseAsync(phaseId);
            return Ok(result);
        }
        [HttpPost("generate-direct-knockout")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> GenerateDirect([FromBody] GenerateDirectKnockoutDto dto)
        {
            try
            {
                // Llamamos al método (asegúrate de que el nombre coincida)
                var phaseId = await _playoffService.GenerateDirectKnockout(dto);

                await _vitrina.NotifyLandingRefreshAsync();
                return Ok(new
                {
                    message = $"Llaves de {dto.PhaseName} generadas exitosamente.",
                    phaseId = phaseId
                });
            }
            catch (Exception ex)
            {
                // 💡 Esto extrae el error real de la base de datos (ej: "null en columna QualifiedCount")
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { error = "Error de Base de Datos", detalle = message });
            }
        }


        /// <summary>
        /// Obtiene un resumen de las fases y jornadas de una competición específica, ordenadas por su orden de juego.
        /// </summary>
        /// <param name="competitionId">El ID de la competición.</param>
        /// <returns>Un resumen de las fases y jornadas de la competición.</returns>
        [HttpGet("competition/{competitionId}/summary")]
        public async Task<IActionResult> GetCompetitionSummary(Guid competitionId)
        {
            var summary = await _context.Phases
                .Where(p => p.CompetitionId == competitionId)
                .OrderBy(p => p.Order)
                .Select(p => new
                {
                    PhaseId = p.Id,
                    PhaseName = p.Name,
                    Order = p.Order,
                    // Traemos el Journal para que saques el ID rápido
                    Journals = _context.Journals
                        .Where(j => j.PhaseId == p.Id)
                        .Select(j => new { j.Id, j.Name })
                        .ToList()
                })
                .ToListAsync();

            if (!summary.Any()) return NotFound("No se encontraron fases para esta competición.");

            return Ok(summary);
        }
    }
}
