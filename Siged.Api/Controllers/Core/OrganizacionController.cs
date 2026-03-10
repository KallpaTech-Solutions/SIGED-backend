using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security;
using Siged.Application.DTOs.Core;
using Siged.Infrastructure.Persistence;
using System.Net.Mime;

namespace Siged.Api.Controllers.Core
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)] // Define que toda la API responde JSON
    public class OrganizacionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrganizacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el catálogo completo de organizaciones.
        /// </summary>
        /// <remarks>
        /// Devuelve tanto unidades activas como inactivas. Es de acceso público para el portal de visitantes.
        /// </remarks>
        /// <response code="200">Lista de organizaciones obtenida correctamente.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<OrganizacionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var organizaciones = await _context.Organizaciones
                .OrderBy(o => o.Nombre)
                .Select(o => new OrganizacionDto
                {
                    Id = o.Id,
                    Nombre = o.Nombre,
                    Abreviatura = o.Abreviatura,
                    Tipo = o.Tipo,
                    ColorRepresentativo = o.ColorRepresentativo,
                    LogoUrl = o.LogoUrl,
                    EstaActivo = o.EstaActivo
                })
                .ToListAsync();

            return Ok(organizaciones);
        }

        /// <summary>
        /// Obtiene la ficha técnica detallada de una organización por su ID.
        /// </summary>
        /// <param name="id">Identificador único de la organización.</param>
        /// <response code="200">Detalle de la organización encontrado.</response>
        /// <response code="404">Si el ID proporcionado no existe.</response>
        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.CoreOrgView)]
        [ProducesResponseType(typeof(Organizacion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var org = await _context.Organizaciones.FindAsync(id);
            if (org == null) return NotFound(new { message = "Organización no encontrada." });

            return Ok(org);
        }

        /// <summary>
        /// Registra una nueva unidad institucional en el sistema SIGED.
        /// </summary>
        /// <remarks>
        /// Realiza validaciones de duplicidad por Nombre y Abreviatura.
        /// </remarks>
        /// <param name="dto">Datos de la nueva organización.</param>
        /// <response code="201">Organización creada con éxito.</response>
        /// <response code="400">Datos inválidos o registro duplicado.</response>
        /// <response code="500">Error interno al procesar en la base de datos.</response>
        [HttpPost]
        [Authorize(Policy = Permissions.CoreOrgManage)]
        [ProducesResponseType(typeof(Organizacion), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] OrganizacionCreateDto dto)
        {
            if (string.IsNullOrEmpty(dto.Nombre) || string.IsNullOrEmpty(dto.Abreviatura))
                return BadRequest(new { message = "El nombre y la abreviatura son obligatorios." });

            var existe = await _context.Organizaciones
                .AnyAsync(o => o.Nombre.ToLower() == dto.Nombre.ToLower() ||
                               o.Abreviatura.ToLower() == dto.Abreviatura.ToLower());

            if (existe)
                return BadRequest(new { message = "Ya existe una facultad con ese nombre o siglas." });

            try
            {
                var nuevaOrg = new Organizacion
                {
                    Nombre = dto.Nombre.Trim(),
                    Abreviatura = dto.Abreviatura.Trim().ToUpper(),
                    Tipo = dto.Tipo ?? "Facultad",
                    Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                    Lema = string.IsNullOrWhiteSpace(dto.Lema) ? null : dto.Lema.Trim(),
                    ColorRepresentativo = string.IsNullOrEmpty(dto.ColorRepresentativo) ? "#0284c7" : dto.ColorRepresentativo,
                    LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim(),
                    PortadaUrl = string.IsNullOrWhiteSpace(dto.PortadaUrl) ? null : dto.PortadaUrl.Trim(),
                    FechaCreacion = dto.FechaCreacion,
                    EstaActivo = dto.EstaActivo
                };

                _context.Organizaciones.Add(nuevaOrg);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = nuevaOrg.Id }, nuevaOrg);
            }
            catch (Exception ex)
            {
                var mensajeReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "Error de base de datos", detail = mensajeReal });
            }
        }

        /// <summary>
        /// Actualiza la información de una organización existente.
        /// </summary>
        /// <param name="id">ID de la unidad a editar.</param>
        /// <param name="dto">Nuevos datos de la organización.</param>
        /// <response code="200">Actualización exitosa.</response>
        /// <response code="404">Organización no encontrada.</response>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.CoreOrgManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] OrganizacionCreateDto dto)
        {
            var org = await _context.Organizaciones.FindAsync(id);
            if (org == null) return NotFound(new { message = "La organización no existe." });

            org.Nombre = dto.Nombre.Trim();
            org.Abreviatura = dto.Abreviatura.Trim().ToUpper();
            org.Tipo = dto.Tipo;
            org.Descripcion = dto.Descripcion?.Trim();
            org.Lema = dto.Lema?.Trim();
            org.ColorRepresentativo = dto.ColorRepresentativo;
            org.LogoUrl = dto.LogoUrl?.Trim();
            org.PortadaUrl = dto.PortadaUrl?.Trim();
            org.FechaCreacion = dto.FechaCreacion;
            org.EstaActivo = dto.EstaActivo;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Organización actualizada con éxito" });
        }

        /// <summary>
        /// Cambia el estado de actividad de una organización (Toggle).
        /// </summary>
        /// <remarks>
        /// Permite habilitar o deshabilitar la unidad en el sistema de forma rápida.
        /// </remarks>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Policy = Permissions.CoreOrgManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var org = await _context.Organizaciones.FindAsync(id);
            if (org == null) return NotFound(new { message = "Organización no encontrada" });

            org.EstaActivo = !org.EstaActivo;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Organización {(org.EstaActivo ? "activada" : "desactivada")} correctamente",
                estaActivo = org.EstaActivo
            });
        }

        /// <summary>
        /// Elimina definitivamente una organización de la base de datos.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.CoreOrgManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var org = await _context.Organizaciones.FindAsync(id);
            if (org == null) return NotFound(new { message = "La organización no existe." });

            // 🛑 VALIDACIÓN: No borrar si tiene usuarios
            bool tieneUsuarios = await _context.Usuarios.AnyAsync(u => u.OrganizacionId == id);
            if (tieneUsuarios)
            {
                return BadRequest(new
                {
                    message = "Operación Denegada",
                    detail = $"No puedes eliminar la unidad '{org.Nombre}' porque tiene usuarios registrados. Desactívala en su lugar."
                });
            }

            try
            {
                _context.Organizaciones.Remove(org); // Borrado físico
                await _context.SaveChangesAsync();
                return Ok(new { message = "Organización eliminada permanentemente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error de integridad", detail = "No se pudo eliminar por dependencias en la base de datos." });
            }
        }
    }
}