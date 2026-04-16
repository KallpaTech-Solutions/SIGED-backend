using Microsoft.AspNetCore.Mvc;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Tournment;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class FixturesController : ControllerBase
    {
        private readonly FixtureService _fixtureService;
        private readonly ApplicationDbContext _context;

        public FixturesController(FixtureService fixtureService, ApplicationDbContext context)
        {
            _fixtureService = fixtureService;
            _context = context;
        }

        [HttpPost("generate-round-robin/{groupId}")]
        public async Task<IActionResult> Generate(Guid groupId)
        {
            // Validar si ya existen jornadas para no duplicar
            var exists = await _context.Journals.AnyAsync(j => j.GroupId == groupId);
            if (exists) return BadRequest("El fixture para este grupo ya ha sido generado.");

            await _fixtureService.GenerateRoundRobin(groupId);
            return Ok(new { message = "Fixture generado exitosamente con Algoritmo Berger." });
        }
    }
}
