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
        private const string DefaultActaLogoLeftKey = "ACTA_DEFAULT_LOGO_LEFT_URL";
        private const string DefaultActaLogoRightKey = "ACTA_DEFAULT_LOGO_RIGHT_URL";
        private const string DisciplineActaLogoLeftRuleKey = "ACTA_LOGO_LEFT_URL";
        private const string DisciplineActaLogoRightRuleKey = "ACTA_LOGO_RIGHT_URL";

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

        [HttpGet("report-assets/default")]
        public async Task<IActionResult> GetDefaultReportAssets()
        {
            return Ok(await BuildReportAssetsResponseAsync(null));
        }

        [HttpPut("report-assets/default")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> UpdateDefaultReportAssets([FromForm] UpdateReportAssetsDto dto)
        {
            // Mismo bucket que iconos de disciplina (evita crear "acta-logos" en Supabase).
            var leftFile = ResolveActaUploadFile(dto.LeftLogoFile, "LeftLogoFile");
            var rightFile = ResolveActaUploadFile(dto.RightLogoFile, "RightLogoFile");
            string? left;
            string? right;
            try
            {
                left = await SaveActaLogoAsync(leftFile, "disciplinas/acta-default-left");
                right = await SaveActaLogoAsync(rightFile, "disciplinas/acta-default-right");
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "No se pudo subir el logo a almacenamiento.", detail = ex.Message });
            }

            try
            {
                await UpsertAppSettingAsync(DefaultActaLogoLeftKey, dto.ClearLeftLogo ? null : left);
                await UpsertAppSettingAsync(DefaultActaLogoRightKey, dto.ClearRightLogo ? null : right);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) when (IsMissingAppSettingsTable(ex))
            {
                return StatusCode(503, new
                {
                    message = "Falta la tabla AppSettings en PostgreSQL. Aplique migraciones EF (por ejemplo: dotnet ef database update --project Siged.Infrastructure --startup-project Siged.Api).",
                    detail = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "No se pudo guardar en base de datos.", detail = ex.InnerException?.Message });
            }

            var response = await BuildReportAssetsResponseAsync(null);
            return Ok(response);
        }

        [HttpGet("{id:guid}/report-assets")]
        public async Task<IActionResult> GetDisciplineReportAssets(Guid id)
        {
            var exists = await _context.Disciplines.AnyAsync(x => x.Id == id);
            if (!exists) return NotFound();
            var response = await BuildReportAssetsResponseAsync(id);
            return Ok(response);
        }

        [HttpPut("{id:guid}/report-assets")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> UpdateDisciplineReportAssets(Guid id, [FromForm] UpdateReportAssetsDto dto)
        {
            var discipline = await _context.Disciplines
                .Include(d => d.Rules)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (discipline == null) return NotFound();

            var leftFile = ResolveActaUploadFile(dto.LeftLogoFile, "LeftLogoFile");
            var rightFile = ResolveActaUploadFile(dto.RightLogoFile, "RightLogoFile");
            string? left;
            string? right;
            try
            {
                left = await SaveActaLogoAsync(leftFile, $"disciplinas/acta-{id}/left");
                right = await SaveActaLogoAsync(rightFile, $"disciplinas/acta-{id}/right");
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "No se pudo subir el logo a almacenamiento.", detail = ex.Message });
            }

            UpsertRule(discipline, DisciplineActaLogoLeftRuleKey, dto.ClearLeftLogo ? null : left);
            UpsertRule(discipline, DisciplineActaLogoRightRuleKey, dto.ClearRightLogo ? null : right);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "No se pudo guardar en base de datos.", detail = ex.InnerException?.Message });
            }

            var response = await BuildReportAssetsResponseAsync(id);
            return Ok(response);
        }

        private async Task<ReportAssetsResponse> BuildReportAssetsResponseAsync(Guid? disciplineId)
        {
            string? left = null;
            string? right = null;

            if (disciplineId.HasValue)
            {
                var rules = await _context.DisciplineRules.AsNoTracking()
                    .Where(r => r.DisciplineId == disciplineId.Value
                        && (r.RuleKey == DisciplineActaLogoLeftRuleKey || r.RuleKey == DisciplineActaLogoRightRuleKey))
                    .ToListAsync();
                left = rules.FirstOrDefault(r => r.RuleKey == DisciplineActaLogoLeftRuleKey)?.RuleValue;
                right = rules.FirstOrDefault(r => r.RuleKey == DisciplineActaLogoRightRuleKey)?.RuleValue;
            }

            string? fallbackLeft = null;
            string? fallbackRight = null;
            try
            {
                fallbackLeft = await _context.AppSettings.AsNoTracking()
                    .Where(x => x.Key == DefaultActaLogoLeftKey)
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync();
                fallbackRight = await _context.AppSettings.AsNoTracking()
                    .Where(x => x.Key == DefaultActaLogoRightKey)
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync();
            }
            catch
            {
                // BD sin migración de AppSettings u otro error de lectura: seguimos sin logos globales.
            }

            return new ReportAssetsResponse
            {
                LeftLogoUrl = left,
                RightLogoUrl = right,
                DefaultLeftLogoUrl = fallbackLeft,
                DefaultRightLogoUrl = fallbackRight,
                EffectiveLeftLogoUrl = !string.IsNullOrWhiteSpace(left) ? left : fallbackLeft,
                EffectiveRightLogoUrl = !string.IsNullOrWhiteSpace(right) ? right : fallbackRight
            };
        }

        /// <summary>
        /// Usa el archivo enlazado al DTO y, si viene vacío, el primero con ese nombre en el multipart
        /// (evita perder el primer archivo cuando el binder solo rellena uno de dos).
        /// </summary>
        private IFormFile? ResolveActaUploadFile(IFormFile? fromDto, string formFieldName)
        {
            if (fromDto is { Length: > 0 })
                return fromDto;
            var fromForm = Request.Form.Files[formFieldName];
            return fromForm is { Length: > 0 } ? fromForm : null;
        }

        private async Task<string?> SaveActaLogoAsync(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;
            var url = await _storageService.UploadFileAsync(file, folder);
            return string.IsNullOrWhiteSpace(url) ? null : url;
        }

        /// <summary>42P01 / mensaje PG cuando aún no se aplicó la migración AddAppSettingsForReportAssets.</summary>
        private static bool IsMissingAppSettingsTable(Exception ex)
        {
            const string marker = "relation \"AppSettings\" does not exist";
            for (Exception? e = ex; e != null; e = e.InnerException)
            {
                if (e.Message.Contains(marker, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private async Task UpsertAppSettingAsync(string key, string? value)
        {
            var row = await _context.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (row != null)
                    _context.AppSettings.Remove(row);
                return;
            }

            if (row == null)
            {
                _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
            }
            else
            {
                row.Value = value;
            }
        }

        private static void UpsertRule(Discipline discipline, string key, string? valueOrNull)
        {
            var row = discipline.Rules.FirstOrDefault(r => r.RuleKey == key);
            if (string.IsNullOrWhiteSpace(valueOrNull))
            {
                if (row != null)
                    discipline.Rules.Remove(row);
                return;
            }

            if (row == null)
            {
                discipline.Rules.Add(new DisciplineRule
                {
                    DisciplineId = discipline.Id,
                    RuleKey = key,
                    RuleValue = valueOrNull
                });
            }
            else
            {
                row.RuleValue = valueOrNull;
            }
        }
    }

    public sealed class UpdateReportAssetsDto
    {
        public IFormFile? LeftLogoFile { get; set; }
        public IFormFile? RightLogoFile { get; set; }
        public bool ClearLeftLogo { get; set; }
        public bool ClearRightLogo { get; set; }
    }

    public sealed class ReportAssetsResponse
    {
        public string? LeftLogoUrl { get; set; }
        public string? RightLogoUrl { get; set; }
        public string? DefaultLeftLogoUrl { get; set; }
        public string? DefaultRightLogoUrl { get; set; }
        public string? EffectiveLeftLogoUrl { get; set; }
        public string? EffectiveRightLogoUrl { get; set; }
    }
}
