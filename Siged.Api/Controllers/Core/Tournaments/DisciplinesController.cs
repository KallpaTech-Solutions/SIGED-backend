using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class DisciplinesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediaStorageService _storageService;

        public DisciplinesController(ApplicationDbContext context, IMediaStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // --- LECTURA ---
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        {
            var query = _context.Disciplines.AsQueryable();
            if (onlyActive) query = query.Where(d => d.IsActive);

            var disciplines = await query.OrderBy(d => d.Name).ToListAsync();
            return Ok(disciplines);
        }

        // --- CREACIÓN ---
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateDisciplineDto dto)
        {
            // 1. Subir icono si existe
            string? iconUrl = null;
            if (dto.IconFile != null)
            {
                iconUrl = await _storageService.UploadFileAsync(dto.IconFile, "disciplinas");
            }

            // 2. Mapear a Entidad
            var discipline = new Discipline
            {
                Name = dto.Name,
                IconUrl = iconUrl,
                IsActive = true
            };

            _context.Disciplines.Add(discipline);
            await _context.SaveChangesAsync();

            return Ok(discipline);
        }

        // --- EDICIÓN ---
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] CreateDisciplineDto dto)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline == null) return NotFound();

            if (dto.IconFile != null)
            {
                discipline.IconUrl = await _storageService.UploadFileAsync(dto.IconFile, "disciplinas");
            }

            discipline.Name = dto.Name;
            // Aquí podrías agregar más campos si los necesitas en el futuro

            await _context.SaveChangesAsync();
            return Ok(discipline);
        }

        // --- ESTADO (Borrado Lógico) ---
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var discipline = await _context.Disciplines.FindAsync(id);
            if (discipline == null) return NotFound();

            discipline.IsActive = !discipline.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { id, isActive = discipline.IsActive });
        }
    }
}
