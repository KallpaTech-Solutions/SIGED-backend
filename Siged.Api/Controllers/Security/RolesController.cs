using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Security;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;

namespace Siged.Api.Controllers.Security
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Lista todos los roles usando el DTO de respuesta para evitar ciclos.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = Permissions.SecurityRoleView)]
        public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
        {
            return await _context.Roles
                .OrderByDescending(r => r.Nivel)
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    Nivel = r.Nivel,
                    UsuariosAsociados = _context.Usuarios.Count(u => u.RolId == r.Id)
                })
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un rol con sus IDs de permisos.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.SecurityRoleView)]
        public async Task<IActionResult> GetRolById(int id)
        {
            var rol = await _context.Roles
                .Include(r => r.Permisos)
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Nombre,
                    r.Descripcion,
                    r.Nivel,
                    Permisos = r.Permisos.Select(p => p.IdPermiso).ToList()
                })
                .FirstOrDefaultAsync();

            return rol == null ? NotFound() : Ok(rol);
        }

        /// <summary>
        /// Crea un rol y devuelve el RoleResponseDto (Soluciona el error 500).
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Permissions.SecurityRoleManage)]
        public async Task<IActionResult> CreateRol([FromBody] RoleSaveDto dto)
        {
            if (await _context.Roles.AnyAsync(r => r.Nombre == dto.Nombre))
                return BadRequest(new { message = "Ya existe un rol con ese nombre." });

            var rol = new Rol
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Nivel = dto.Nivel
            };

            if (dto.Permisos.Any())
            {
                rol.Permisos = await _context.Permisos
                    .Where(p => dto.Permisos.Contains(p.IdPermiso))
                    .ToListAsync();
            }

            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            // ✅ Mapeamos a RoleResponseDto para romper el ciclo de serialización
            var response = new RoleResponseDto
            {
                Id = rol.Id,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion,
                Nivel = rol.Nivel,
                UsuariosAsociados = 0
            };

            return CreatedAtAction(nameof(GetRoles), new { id = rol.Id }, response);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.SecurityRoleManage)]
        public async Task<IActionResult> UpdateRol(int id, [FromBody] RoleSaveDto dto)
        {
            var rol = await _context.Roles.Include(r => r.Permisos).FirstOrDefaultAsync(r => r.Id == id);
            if (rol == null) return NotFound();

            if (rol.Nombre == "SuperAdmin" && dto.Nombre != "SuperAdmin")
                return BadRequest(new { message = "No se puede renombrar el rol raíz." });

            rol.Nombre = dto.Nombre;
            rol.Descripcion = dto.Descripcion;
            rol.Nivel = dto.Nivel;

            rol.Permisos.Clear();
            rol.Permisos = await _context.Permisos
                .Where(p => dto.Permisos.Contains(p.IdPermiso))
                .ToListAsync();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Rol actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.SecurityRoleManage)]
        public async Task<IActionResult> DeleteRol(int id)
        {
            var rol = await _context.Roles.FindAsync(id);
            if (rol == null) return NotFound();

            if (rol.Nombre == "SuperAdmin")
                return BadRequest(new { message = "El rol SuperAdmin no puede eliminarse." });

            if (await _context.Usuarios.AnyAsync(u => u.RolId == id))
                return BadRequest(new { message = "El rol tiene usuarios vinculados." });

            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Rol eliminado exitosamente." });
        }
    }
}