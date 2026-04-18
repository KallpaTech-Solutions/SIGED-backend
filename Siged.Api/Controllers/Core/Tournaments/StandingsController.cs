using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Services.Tournment;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StandingsController : ControllerBase
{
    private readonly StandingsService _standingsService;
    public StandingsController(StandingsService standingsService) => _standingsService = standingsService;

    /// <summary>
    /// Obtiene la tabla de posiciones de un grupo.
    /// </summary>
    [HttpGet("group/{groupId}")]
    [AllowAnonymous] // 👈 El público debe poder ver la tabla sin loguearse
    public async Task<IActionResult> GetStandings(Guid groupId)
    {
        var result = await _standingsService.GetStandingsByGroupAsync(groupId);
        return Ok(result);
    }

    /// <summary>
    /// RECALCULO FORZADO: Útil si hubo ediciones manuales en los resultados.
    /// </summary>
    [HttpPost("group/{groupId}/recalculate")]
    [Authorize(Policy = Permissions.TournManage)] // 👈 Solo el administrador
    public async Task<IActionResult> Recalculate(Guid groupId)
    {
        await _standingsService.UpdateGroupStandingsAsync(groupId);
        return Ok(new { message = "Tabla de posiciones recalculada y persistida." });
    }
}