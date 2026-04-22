using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core.Notice;
using Siged.Domain.Entities.Core.Tournaments;
using Siged.Domain.Entities.Core.Tournaments.Enums;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Admin
{
    /// <summary>
    /// Métricas y resúmenes para el Panel de Control (Dashboard).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context) => _context = context;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] string? blocks)
        {
            try
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

                if (string.IsNullOrEmpty(blocks))
                {
                    var pref = await _context.UserPreferences.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UserId == userId);
                    blocks = pref?.WidgetsVisibles ?? "all";
                }

                var requested = blocks.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var isAll = requested.Contains("all");
                var result = new Dictionary<string, object>();

                // --- Usuarios ---
                if (User.HasClaim("permission", "security.user.view"))
                {
                    if (isAll || requested.Contains("usuarios"))
                        result["totalUsuarios"] = await _context.Usuarios.CountAsync();

                    if (isAll || requested.Contains("activos"))
                        result["usuariosActivos"] = await _context.Usuarios.CountAsync(u => u.EstaActivo);

                    if (isAll || requested.Contains("recent"))
                    {
                        var ultimos = await _context.Usuarios
                            .AsNoTracking()
                            .Include(u => u.Persona)
                            .OrderByDescending(u => u.FechaRegistro)
                            .Take(5)
                            .Select(u => u.Persona != null
                                ? $"{u.Persona.Nombres} {u.Persona.Apellidos}"
                                : u.Username)
                            .ToListAsync();
                        result["ultimosUsuarios"] = ultimos;
                    }
                }

                // --- Organizaciones ---
                if (User.HasClaim("permission", "core.org.view"))
                {
                    if (isAll || requested.Contains("orgs") || requested.Contains("organizaciones"))
                        result["totalFacultades"] = await _context.Organizaciones.CountAsync();
                }

                // --- Torneos (métrica + tarjeta destacada) ---
                var canTourn = User.HasClaim("permission", "comp.tourn.view") ||
                               User.HasClaim("permission", "tourn.view") ||
                               User.HasClaim("permission", "tourn.manage");
                if (canTourn)
                {
                    if (isAll || requested.Contains("torneos"))
                    {
                        // Prioridad: inscripciones o en curso; si no hay ninguno, contar en planeamiento (borrador visible)
                        var enInscripcionOCurso = await _context.Tournaments.CountAsync(t =>
                            t.IsActive &&
                            (t.Status == TournamentStatus.InscripcionesAbiertas ||
                             t.Status == TournamentStatus.Activo ||
                             t.Status == TournamentStatus.Programado));

                        if (enInscripcionOCurso > 0)
                        {
                            result["totalTorneos"] = enInscripcionOCurso;
                            result["torneosMetricaContexto"] = "inscripciones_curso";
                        }
                        else
                        {
                            var enPlaneamiento = await _context.Tournaments.CountAsync(t =>
                                t.IsActive && t.Status == TournamentStatus.Borrador);
                            result["totalTorneos"] = enPlaneamiento;
                            result["torneosMetricaContexto"] =
                                enPlaneamiento > 0 ? "planeamiento" : "ninguno";
                        }
                    }

                    if (isAll || requested.Contains("banner_torneos"))
                    {
                        // Tres torneos más recientes visibles en vitrina (IsActive), misma lógica que el listado público
                        var torneosLista = await _context.Tournaments
                            .AsNoTracking()
                            .Where(t => t.IsActive)
                            .OrderByDescending(t => t.CreatedAt)
                            .Take(3)
                            .Select(t => new
                            {
                                t.Id,
                                t.Name,
                                t.Year,
                                status = t.Status,
                                t.Organizer,
                                t.StartDate,
                                t.EndDate,
                                t.CreatedAt
                            })
                            .ToListAsync();
                        result["torneosActivos"] = torneosLista;
                    }
                }

                // --- Noticias (última o últimas 3 publicadas) ---
                if (User.HasClaim("permission", "news.view"))
                {
                    var take = 0;
                    if (requested.Contains("noticias_3")) take = 3;
                    else if (requested.Contains("noticias_1")) take = 1;

                    if (take > 0)
                    {
                        var newsRows = await _context.News
                            .AsNoTracking()
                            .Include(n => n.Media)
                            .Where(n => n.Status == NewsStatus.Published)
                            .OrderByDescending(n => n.IsFeatured)
                            .ThenByDescending(n => n.CreatedAt)
                            .Take(take)
                            .ToListAsync();

                        result["noticiasInicio"] = newsRows.Select(n => new
                        {
                            n.Id,
                            n.Title,
                            n.Excerpt,
                            n.Slug,
                            n.CreatedAt,
                            imageUrl = n.Media.OrderBy(m => m.Id).Select(m => m.Url).FirstOrDefault()
                        }).ToList();
                        result["noticiasModo"] = take;
                    }
                }

                result["tipoVista"] = "global";
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
