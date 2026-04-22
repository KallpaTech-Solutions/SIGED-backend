using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Discipline;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Constants;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;
using System.Net.Mime;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bloqueado por defecto para seguridad SIGED
    [Produces(MediaTypeNames.Application.Json)] // Asegura que todas las respuestas sean JSON
    public class DisciplinesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public DisciplinesController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        /// <summary>
        /// Obtiene el catálogo de disciplinas deportivas.
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Permitir acceso público para listar disciplinas
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        {
            var query = _context.Disciplines.AsQueryable();
            if (onlyActive) query = query.Where(d => d.IsActive);

            var disciplines = await query.OrderBy(d => d.Name).ToListAsync();
            return Ok(disciplines);
        }
        /// <summary>
        /// Obtiene una disciplina específica por su ID.
        /// </summary>
        /// <param name="id">ID de la disciplina.</param>
        /// <returns>La disciplina encontrada.</returns>
        /// <response code="200">La disciplina fue encontrada exitosamente.</response>
        /// <response code="404">No se encontró una disciplina con el ID proporcionado.</response>
        /// <response code="500">Ocurrió un error interno al obtener la disciplina.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline == null) return NotFound();
            return Ok(discipline);
        }

        /// <summary>
        /// Crea una nueva disciplina deportiva.
        /// </summary>
        /// <param name="dto">Datos de la disciplina a crear.</param>
        /// <returns>La disciplina creada.</returns>
        /// <response code="200">La disciplina fue creada exitosamente.</response>
        /// <response code="400">Los datos proporcionados son inválidos.</response>
        /// <response code="500">Ocurrió un error interno al crear la disciplina.</response>
        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create([FromForm] CreateDisciplineDto dto)
        {
            // 1. Buscamos la plantilla oficial (FIFA_FUTSAL, etc.)
            if (!SportRulesTemplates.OfficialTemplates.TryGetValue(dto.TemplateKey, out var template))
                return BadRequest("La plantilla seleccionada no existe.");

            string? iconUrl = dto.IconFile != null
                ? await _storageService.UploadFileAsync(dto.IconFile, "disciplinas")
                : null;

            var discipline = new Discipline
            {
                Name = dto.Name,
                IconUrl = iconUrl,
                TemplateKey = dto.TemplateKey,
                IsActive = true,
                ScoringType = template.Type // ⬅️ IMPORTANTE: Asignamos el tipo de la plantilla
            };

            // 2. Generamos las reglas base usando tu Service
            var ruleService = new DisciplineRuleService(); // O inyéctalo en el constructor
            discipline.Rules = ruleService.GetOfficialTemplate(dto.TemplateKey, discipline.Id);

            _context.Disciplines.Add(discipline);
            await _context.SaveChangesAsync();

            return Ok(discipline);
        }

        /// <summary>
        /// Actualiza una disciplina existente.
        /// </summary>
        /// <param name="id">ID de la disciplina a actualizar.</param>
        /// <param name="dto">Datos de la disciplina a actualizar.</param>
        /// <returns>La disciplina actualizada.</returns>
        /// <response code="200">La disciplina fue actualizada exitosamente.</response>
        /// <response code="404">No se encontró una disciplina con el ID proporcionado.</response>
        /// <response code="500">Ocurrió un error interno al actualizar la disciplina.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateDisciplineDto dto)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline == null) return NotFound();

            if (dto.IconFile != null)
            {
                discipline.IconUrl = await _storageService.UploadFileAsync(dto.IconFile, "disciplinas");
            }

            discipline.Name = dto.Name;

            await _context.SaveChangesAsync();
            return Ok(discipline);
        }

        /// <summary>
        /// Cambia el estado activo/inactivo de una disciplina.
        /// </summary>
        /// <param name="id">ID de la disciplina cuyo estado se desea cambiar.</param>
        /// <returns>El ID de la disciplina y su nuevo estado.</returns>
        /// <response code="200">El estado de la disciplina fue cambiado exitosamente.</response>
        /// <response code="404">No se encontró una disciplina con el ID proporcionado.</response>
        /// <response code="500">Ocurrió un error interno al cambiar el estado de la disciplina.</response>
        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline == null) return NotFound();

            discipline.IsActive = !discipline.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { id, isActive = discipline.IsActive });
        }
        /// <summary>
        /// Eliminación definitiva (Solo si no tiene competencias asociadas).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline == null) return NotFound();

            // 🔍 Buscamos si existe alguna competencia que use esta disciplina
            var hasCompetitions = await _context.Competitions.AnyAsync(c => c.DisciplineId == id);

            if (hasCompetitions)
                return BadRequest("No se puede eliminar la disciplina porque está vinculada a competencias activas.");

            _context.Disciplines.Remove(discipline);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- GESTIÓN DE REGLAS MAESTRAS ---

        /// <summary>
        /// Obtiene las reglas configuradas para una disciplina.
        /// </summary>
        [HttpGet("{id}/rules")]
        [AllowAnonymous]// Permitir acceso público para consultar reglas maestras
        public async Task<IActionResult> GetDisciplineRules(Guid id)
        {
            var rules = await _context.DisciplineRules
                .Where(r => r.DisciplineId == id)
                .ToListAsync();
            return Ok(rules);
        }

        /// <summary>
        /// Actualiza las reglas configuradas para una disciplina.
        /// </summary>
        [HttpPut("{id}/rules")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> UpdateDisciplineRules(Guid id, [FromBody] List<DisciplineRuleDto> rulesDto)
        {
            var discipline = await _context.Disciplines.Include(d => d.Rules).FirstOrDefaultAsync(d => d.Id == id);
            if (discipline == null) return NotFound();

            // Sincronización: Borramos las anteriores y grabamos las nuevas
            _context.DisciplineRules.RemoveRange(discipline.Rules);

            foreach (var dto in rulesDto)
            {
                _context.DisciplineRules.Add(new DisciplineRule
                {
                    DisciplineId = id,
                    RuleKey = dto.RuleKey.ToUpper(),
                    RuleValue = dto.RuleValue
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Reglas maestras actualizadas." });
        }
    }
}
