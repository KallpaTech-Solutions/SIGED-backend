using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Security;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermisosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermisosController(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Lista el catálogo de permisos disponibles agrupados por categoría.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.SecurityRoleView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermisoDTO>>> GetPermisos()
    {
        return await _context.Permisos
            .OrderBy(p => p.Categoria)
            .Select(p => new PermisoDTO // ✅ Uso de DTO para evitar basura en el JSON
            {
                IdPermiso = p.IdPermiso,
                Categoria = p.Categoria,
                Descripcion = p.Descripcion
            })
            .ToListAsync();
    }
}