using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Core;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using System.Net.Mime;

namespace Siged.Api.Controllers.Core
{
    /// <summary>
    /// Controlador para la administración del catálogo de Dependencias (Oficinas Administrativas).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    public class DependenciasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DependenciasController(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// Obtiene el listado completo de oficinas administrativas registradas en el sistema.
        /// </summary>
        /// <remarks>
        /// 💡 Este endpoint es utilizado para alimentar los selectores de "Oficina" en el registro de personal administrativo.
        /// </remarks>
        /// <response code="200">Retorna la lista de dependencias ordenadas alfabéticamente.</response>
        /// <response code="401">No se detectó un token de autenticación válido.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<DependenciaDTO>>> GetDependencias()
        {
            return await _context.Dependencias
                .OrderBy(d => d.Nombre)
                .Select(d => new DependenciaDTO
                {
                    Id = d.Id,
                    Nombre = d.Nombre,
                    Siglas = d.Siglas ?? string.Empty // Seguridad contra nulos
                })
                .ToListAsync();
        }

        /// <summary>
        /// Registra una nueva oficina administrativa en el catálogo institucional.
        /// </summary>
        /// <param name="dto">Datos de la nueva oficina (Nombre y Siglas).</param>
        /// <remarks>
        /// 🛡️ **Regla de Negocio:** No se permite duplicidad de nombres para evitar confusiones en el organigrama.
        /// </remarks>
        /// <response code="201">Oficina creada exitosamente.</response>
        /// <response code="400">El nombre de la oficina ya existe o los datos son inválidos.</response>
        /// <response code="403">El usuario no tiene el permiso 'Security.User.Manage'.</response>
        [HttpPost]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateDependencia([FromBody] DependenciaDTO dto)
        {
            if (await _context.Dependencias.AnyAsync(d => d.Nombre == dto.Nombre))
                return BadRequest(new { message = "Ya existe una dependencia registrada con ese nombre." });

            var dependencia = new Dependencia
            {
                Nombre = dto.Nombre,
                Siglas = dto.Siglas
            };

            _context.Dependencias.Add(dependencia);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDependencias), new { id = dependencia.Id }, dependencia);
        }

        /// <summary>
        /// Actualiza la información (Nombre o Siglas) de una oficina existente.
        /// </summary>
        /// <param name="id">ID único de la dependencia.</param>
        /// <param name="dto">Nuevos datos a aplicar.</param>
        /// <response code="200">Cambios guardados con éxito.</response>
        /// <response code="404">La oficina solicitada no existe.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDependencia(int id, [FromBody] DependenciaDTO dto)
        {
            var dependencia = await _context.Dependencias.FindAsync(id);
            if (dependencia == null) return NotFound(new { message = "Dependencia no encontrada." });

            dependencia.Nombre = dto.Nombre;
            dependencia.Siglas = dto.Siglas;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Información de la oficina actualizada correctamente." });
        }

        /// <summary>
        /// Elimina una oficina del sistema de forma lógica/física.
        /// </summary>
        /// <remarks>
        /// 🛡️ **Protección de Integridad:** Solo se puede eliminar si no hay administradores asignados a esta oficina.
        /// </remarks>
        /// <param name="id">ID de la oficina a eliminar.</param>
        /// <response code="200">Eliminación exitosa.</response>
        /// <response code="400">Error: La oficina tiene personal asignado y no puede borrarse.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteDependencia(int id)
        {
            var dependencia = await _context.Dependencias
                .Include(d => d.Administradores)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dependencia == null) return NotFound();

            if (dependencia.Administradores.Any())
            {
                return BadRequest(new
                {
                    message = $"Operación cancelada: La oficina '{dependencia.Nombre}' tiene {dependencia.Administradores.Count} usuarios vinculados."
                });
            }

            _context.Dependencias.Remove(dependencia);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Dependencia eliminada del catálogo institucional." });
        }
    }
}