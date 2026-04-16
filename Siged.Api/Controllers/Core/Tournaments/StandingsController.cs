using Microsoft.AspNetCore.Mvc;
using Siged.Infrastructure.Services.Tournment;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Siged.Api.Controllers.Core.Tournaments
{
    [Route("api/[controller]")]
    [ApiController]
    public class StandingsController : ControllerBase
    {
        private readonly StandingsService _standingsService;
        public StandingsController(StandingsService standingsService) => _standingsService = standingsService;

        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetStandings(Guid groupId)
        {
            var result = await _standingsService.GetStandingsByGroupAsync(groupId);
            return Ok(result);
        }
    }
}
