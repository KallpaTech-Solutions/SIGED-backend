using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;


namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompetitionsController : ControllerBase
    {

        private readonly ApplicationDbContext _context;

        public CompetitionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCompetitionDto dto)
        {
            // 1. Validar que no exista ya esa combinación (Torneo + Disciplina + Género)
            var exists = await _context.Competitions.AnyAsync(c =>
                c.TournamentId == dto.TournamentId &&
                c.DisciplineId == dto.DisciplineId &&
                c.Gender == dto.Gender);

            if (exists)
                return BadRequest("Esta competición (Deporte + Género) ya está registrada en este torneo.");

            // 2. Crear la entidad
            var competition = new Competition
            {
                TournamentId = dto.TournamentId,
                DisciplineId = dto.DisciplineId,
                Gender = dto.Gender,
                CategoryName = dto.CategoryName
            };

            _context.Competitions.Add(competition);
            await _context.SaveChangesAsync();

            // Retornamos con Include para que el Front vea el nombre de la disciplina
            var result = await _context.Competitions
                .Include(c => c.Discipline)
                .FirstOrDefaultAsync(c => c.Id == competition.Id);

            return Ok(result);
        }

        [HttpGet("tournament/{tournamentId}")]
        public async Task<IActionResult> GetByTournament(Guid tournamentId)
        {
            var competitions = await _context.Competitions
                .Include(c => c.Discipline)
                .Where(c => c.TournamentId == tournamentId)
                .ToListAsync();

            return Ok(competitions);
        }
        // 1. EDITAR: Cambiar datos (Deporte, Género, Categoría)
        // Modifica tu Update para que no cree duplicados por error
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateCompetitionDto dto)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null) return NotFound();

            // Validar que el cambio no choque con otra competencia existente
            var duplicate = await _context.Competitions.AnyAsync(c =>
                c.Id != id &&
                c.TournamentId == competition.TournamentId &&
                c.DisciplineId == dto.DisciplineId &&
                c.Gender == dto.Gender);

            if (duplicate) return BadRequest("Ya existe otra competición con ese Deporte y Género en este torneo.");

            competition.DisciplineId = dto.DisciplineId;
            competition.Gender = dto.Gender;
            competition.CategoryName = dto.CategoryName;

            await _context.SaveChangesAsync();
            return Ok(competition);
        }

        // 2. CAMBIAR ESTADO: Activar o Desactivar (Soft Delete/Restore)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null) return NotFound();

            competition.IsActive = !competition.IsActive; // Si es true pasa a false, y viceversa
            await _context.SaveChangesAsync();

            return Ok(new { id = competition.Id, isActive = competition.IsActive });
        }

        // 3. ELIMINAR FÍSICAMENTE: Solo para errores de creación (Hard Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var competition = await _context.Competitions
                .Include(c => c.Phases) // Cargamos las fases para verificar
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition == null) return NotFound();

            // 🛡️ REGLA DE ORO DE INGENIERÍA:
            // No permitas borrar si ya tiene datos relacionados (fases, grupos, etc.)
            if (competition.Phases.Any())
            {
                return BadRequest("No se puede eliminar físicamente porque ya tiene fases configuradas. Use la desactivación en su lugar.");
            }

            _context.Competitions.Remove(competition);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var competition = await _context.Competitions
                .Include(c => c.Discipline)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (competition == null) return NotFound();
            return Ok(competition);
        }
    }
}
