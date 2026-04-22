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
    /// <summary>
    /// Catálogo de sedes (canchas, estadios). Los <see cref="Match"/> referencian <see cref="Venue"/> al programar.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    public class VenuesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VenuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] Guid? complexId)
        {
            var q = _context.Venues.AsNoTracking().Include(v => v.Complex).AsQueryable();
            if (complexId.HasValue) q = q.Where(v => v.ComplexId == complexId);

            var list = await q.OrderBy(v => v.Name).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var v = await _context.Venues
                .AsNoTracking()
                .Include(x => x.Complex)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();
            return Ok(v);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Create([FromBody] CreateVenueDto dto)
        {
            if (dto.ComplexId.HasValue
                && !await _context.VenueComplexes.AnyAsync(c => c.Id == dto.ComplexId.Value))
                return BadRequest("El complejo indicado no existe.");

            var entity = new Venue
            {
                Name = dto.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                Capacity = dto.Capacity,
                ComplexId = dto.ComplexId,
            };
            _context.Venues.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVenueDto dto)
        {
            var entity = await _context.Venues.FindAsync(id);
            if (entity == null) return NotFound();

            if (dto.ComplexId.HasValue
                && !await _context.VenueComplexes.AnyAsync(c => c.Id == dto.ComplexId.Value))
                return BadRequest("El complejo indicado no existe.");

            entity.Name = dto.Name.Trim();
            entity.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            entity.Capacity = dto.Capacity;
            entity.ComplexId = dto.ComplexId;
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.TournManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.Venues.FindAsync(id);
            if (entity == null) return NotFound();

            var inUse = await _context.Matches.AnyAsync(m => m.VenueId == id);
            if (inUse)
                return BadRequest("No se puede eliminar: hay partidos programados en esta sede.");

            _context.Venues.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
