using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Tournaments.Venue;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using System.Net.Mime;

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    public class VenueComplexesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VenueComplexesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        {
            var q = _context.VenueComplexes.AsNoTracking().AsQueryable();
            if (onlyActive) q = q.Where(c => c.IsActive);
            var list = await q.OrderBy(c => c.Name).ToListAsync();
            return Ok(list);
        }

        /// <summary>Complejos con sus canchas (para selects agrupados en UI).</summary>
        [HttpGet("with-venues")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllWithVenues([FromQuery] bool onlyActive = true)
        {
            var q = _context.VenueComplexes.AsNoTracking().AsQueryable();
            if (onlyActive) q = q.Where(c => c.IsActive);

            var list = await q
                .Include(c => c.Venues)
                .OrderBy(c => c.Name)
                .AsSplitQuery()
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var c = await _context.VenueComplexes
                .AsNoTracking()
                .Include(x => x.Venues)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create([FromBody] CreateVenueComplexDto dto)
        {
            var entity = new VenueComplex
            {
                Name = dto.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? null : dto.ContactName.Trim(),
                ContactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone) ? null : dto.ContactPhone.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim(),
                MapUrl = string.IsNullOrWhiteSpace(dto.MapUrl) ? null : dto.MapUrl.Trim(),
                OpeningHoursNote = string.IsNullOrWhiteSpace(dto.OpeningHoursNote) ? null : dto.OpeningHoursNote.Trim(),
                IsActive = dto.IsActive,
            };
            _context.VenueComplexes.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVenueComplexDto dto)
        {
            var entity = await _context.VenueComplexes.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Name = dto.Name.Trim();
            entity.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            entity.ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? null : dto.ContactName.Trim();
            entity.ContactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone) ? null : dto.ContactPhone.Trim();
            entity.ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim();
            entity.MapUrl = string.IsNullOrWhiteSpace(dto.MapUrl) ? null : dto.MapUrl.Trim();
            entity.OpeningHoursNote = string.IsNullOrWhiteSpace(dto.OpeningHoursNote) ? null : dto.OpeningHoursNote.Trim();
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.VenueComplexes
                .Include(c => c.Venues)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();

            if (entity.Venues.Any())
                return BadRequest("No se puede eliminar: hay sedes/canchas asociadas a este complejo. Reasígnelas o elimínelas primero.");

            _context.VenueComplexes.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
