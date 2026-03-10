using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Core;
using Siged.Application.DTOs.Security;
using Siged.Application.Interfaces.Security;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using System.Security.Claims;

namespace Siged.Api.Controllers.Security
{
    /// <summary>
    /// Controlador para la gestión integral de usuarios, perfiles y seguridad jerárquica.
    /// Permite administrar la jerarquía de usuarios y la matriz de permisos híbrida (Rol + Especiales).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUsuarioService _usuarioService;
        private readonly IPasswordHasher _passwordHasher;

        public UsuariosController(ApplicationDbContext context, IUsuarioService usuarioService, IPasswordHasher passwordHasher)
        {
            _context = context;
            _usuarioService = usuarioService;
            _passwordHasher = passwordHasher;
        }

        #region Utilidades Privadas
        private int GetUserIdFromToken()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
        #endregion

        #region Escritura (Creación y Edición)

        /// <summary>
        /// Registra un nuevo usuario y su perfil específico (Admin/Est/Enc) en una transacción atómica.
        /// </summary>
        /// <response code="201">Usuario creado exitosamente.</response>
        /// <response code="403">Jerarquía insuficiente.</response>
        [HttpPost("registrar")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UserCreateDto dto)
        {
            var ejecutorId = GetUserIdFromToken();
            var ejecutor = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == ejecutorId);
            if (ejecutor?.Rol == null)
            {
                return Unauthorized(new { message = "Sesión inválida o sin privilegios." });
            }

            var rolDestino = await _context.Roles.FindAsync(dto.RolId);
            if (rolDestino == null) return BadRequest(new { message = "Rol inexistente." });

            if (ejecutor.Rol.Nombre != "SuperAdmin" && rolDestino.Nivel >= ejecutor.Rol.Nivel)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No puedes crear usuarios de nivel igual o superior." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Persona persona;
                // Fábrica de personas con inicialización de campos requeridos (Fix CS9035)
                if (dto.RolId == 1 || dto.RolId == 2)
                {
                    persona = new Administrador { 
                        DNI = dto.DNI, 
                        Nombres = dto.Nombres, 
                        Apellidos = dto.Apellidos, 
                        Correo = dto.Correo, 
                        DependenciaId = dto.DependenciaId, 
                        EsPersonalInterno = dto.EsPersonalInterno 
                    };
                }
                else if (dto.RolId == 4)
                {
                    persona = new Estudiante { DNI = dto.DNI, Nombres = dto.Nombres, Apellidos = dto.Apellidos, Correo = dto.Correo, CodigoEstudiante = dto.CodigoEstudiante ?? "", EstaMatriculado = true };
                }
                else
                {
                    persona = new Encargado { DNI = dto.DNI, Nombres = dto.Nombres, Apellidos = dto.Apellidos, Correo = dto.Correo, Cargo = dto.Cargo ?? "Personal", Oficina = dto.Oficina ?? "N/A" };
                }

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                var usuario = new Usuario
                {
                    Username = dto.Username,
                    PasswordHash = _passwordHasher.Hash(dto.DNI),
                    RolId = dto.RolId,
                    PersonaId = persona.Id,
                    OrganizacionId = dto.OrganizacionId,
                    CreadoPorUsuarioId = ejecutor.Id,
                    EstaActivo = true,
                    RequiereCambioPassword = true,
                    FechaRegistro = DateTime.UtcNow
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetUsuarioById), new { id = usuario.Id }, new { message = "Registrado correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "Fallo en transacción.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un usuario y su perfil, protegiendo la jerarquía institucional.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] UsuarioUpdateDto dto)
        {
            var ejecutorId = GetUserIdFromToken();
            var ejecutor = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == ejecutorId);
            var usuario = await _context.Usuarios.Include(u => u.Persona).Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);

            if (usuario?.Persona == null || ejecutor?.Rol == null) return NotFound();

            if (ejecutor.Rol.Nombre != "SuperAdmin" && usuario.Rol.Nivel >= ejecutor.Rol.Nivel)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "No puedes editar a un superior." });

            // Mapeo dinámico
            usuario.Username = dto.Username ?? usuario.Username;
            usuario.Persona.Nombres = dto.Nombres ?? usuario.Persona.Nombres;
            usuario.Persona.Apellidos = dto.Apellidos ?? usuario.Persona.Apellidos;
            usuario.Persona.Correo = dto.Correo ?? usuario.Persona.Correo;
            usuario.Persona.DNI = dto.Dni ?? usuario.Persona.DNI;

            if (usuario.Persona is Administrador admin && dto.DependenciaId.HasValue) admin.DependenciaId = dto.DependenciaId.Value;
            else if (usuario.Persona is Estudiante est && !string.IsNullOrWhiteSpace(dto.CodigoEstudiante)) est.CodigoEstudiante = dto.CodigoEstudiante;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Actualizado correctamente." });
        }

        #endregion

        #region Lectura (Listados y Detalles)

        /// <summary>
        /// Obtiene el listado de usuarios con su identificación institucional (Siglas de Oficina o Facultad).
        /// </summary>
        /// <remarks>
        /// 💡 **Nota técnica:** /// Utiliza proyecciones optimizadas para AWS RDS y maneja la jerarquía TPH para mostrar 
        /// la procedencia correcta (Dependencia para Admins, Organización para el resto).
        /// </remarks>
        /// <param name="organizacionId">Opcional: ID de la Facultad para filtrar los resultados.</param>
        /// <response code="200">Retorna la lista de usuarios procesada para el Frontend.</response>
        [HttpGet]
        [Authorize(Policy = Permissions.SecurityUserView)]
        public async Task<ActionResult<IEnumerable<UsuarioDTO>>> GetUsuarios([FromQuery] int? organizacionId)
        {
            var query = _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Organizacion)
                .Include(u => u.Persona)
                .AsQueryable();

            if (organizacionId.HasValue && organizacionId > 0)
                query = query.Where(u => u.OrganizacionId == organizacionId.Value);

            #pragma warning disable CS8601 // Ignorar posible nulo en proyecciones
            var usuarios = await query
                .Where(u => u.Persona != null && u.Rol != null)
                .OrderByDescending(u => u.FechaRegistro)
                .Select(u => new UsuarioDTO
                {
                    Id = u.Id,
                    Username = u.Username,
                    NombreCompleto = $"{u.Persona.Nombres} {u.Persona.Apellidos}",
                    Dni = u.Persona.DNI,
                    Rol = u.Rol.Nombre,
                    EstaActivo = u.EstaActivo,
                    FotoPath = u.Persona.FotoPath, // ✅ Ahora la lista también tiene fotos
                    OrganizacionId = u.OrganizacionId,
                    DependenciaId = (u.Persona is Administrador) ? ((Administrador)u.Persona).DependenciaId : null,
                    Entidad = u.Persona is Administrador
                        ? (((Administrador)u.Persona).Dependencia != null ? ((Administrador)u.Persona).Dependencia!.Siglas : "S/D")
                        : (u.Organizacion != null ? u.Organizacion.Abreviatura : "SEDE CENTRAL"),
                    NombreInstitucion = u.Persona is Administrador
                        ? (((Administrador)u.Persona).Dependencia != null ? ((Administrador)u.Persona).Dependencia!.Nombre : "OFICINA CENTRAL")
                        : (u.Organizacion != null ? u.Organizacion.Nombre : "UNAS")
                }).ToListAsync();
                #pragma warning restore CS8601

            return Ok(usuarios);
        }

        /// <summary>
        /// Detalle completo de un usuario con matriz de permisos híbrida.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.SecurityUserView)]
        public async Task<IActionResult> GetUsuarioById(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Persona)
                .Include(u => u.Rol).ThenInclude(r => r.Permisos)
                .Include(u => u.PermisosEspeciales)
                .Include(u => u.Organizacion)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            object? detalles = null;
            if (usuario.Persona is Administrador admin)
                detalles = new { Tipo = "Administrador", admin.DependenciaId, admin.EsPersonalInterno };
            else if (usuario.Persona is Estudiante est)
                detalles = new { Tipo = "Estudiante", est.CodigoEstudiante, est.EstaMatriculado };
            else if (usuario.Persona is Encargado enc)
                detalles = new { Tipo = "Encargado", enc.Cargo, enc.Oficina };

            return Ok(new
            {
                usuario.Id,
                usuario.Username,
                usuario.EstaActivo,
                usuario.FechaRegistro,
                Rol = usuario.Rol.Nombre,
                Persona = new
                {
                    usuario.Persona.DNI,
                    usuario.Persona.Nombres,
                    usuario.Persona.Apellidos,
                    usuario.Persona.Correo,
                    usuario.Persona.FotoPath, // ✅ Clave para que se vea en la ficha
                    Detalles = detalles
                },
                Organizacion = new
                {
                    Id = usuario.OrganizacionId,
                    Nombre = usuario.Organizacion?.Nombre ?? "UNAS - SEDE CENTRAL"
                },
                PermisosDelRol = usuario.Rol.Permisos.Select(p => p.IdPermiso).ToList(),
                PermisosDirectos = usuario.PermisosEspeciales.Select(p => p.IdPermiso).ToList()
            });
        }

        #endregion

        #region Seguridad y Estado

        /// <summary>
        /// Asigna permisos especiales que se suman a los del rol.
        /// </summary>
        [HttpPost("asignar-permisos-directos")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        public async Task<IActionResult> AsignarPermisosDirectos([FromBody] AsignarPermisosDirectosDto dto)
        {
            var usuario = await _context.Usuarios.Include(u => u.PermisosEspeciales).FirstOrDefaultAsync(u => u.Id == dto.UsuarioId);
            if (usuario == null) return NotFound();

            usuario.PermisosEspeciales = await _context.Permisos.Where(p => dto.Permisos.Contains(p.IdPermiso)).ToListAsync();
            await _context.SaveChangesAsync();
            return Ok(new { message = "Permisos directos actualizados." });
        }

        /// <summary>
        /// Cambia el estado de actividad del usuario.
        /// </summary>
        [HttpPatch("{id}/estado")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        public async Task<IActionResult> CambiarEstado(int id, [FromQuery] bool activo)
        {
            var ejecutorId = GetUserIdFromToken();
            var ejecutor = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == ejecutorId);
            var objetivo = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);

            if (objetivo == null || ejecutor == null) return NotFound();
            if (ejecutor.Rol.Nombre != "SuperAdmin" && objetivo.Rol.Nivel >= ejecutor.Rol.Nivel) return Forbid();

            objetivo.EstaActivo = activo;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Estado actualizado." });
        }

        /// <summary>
        /// Reinicia password al DNI del usuario (Jerarquía protegida).
        /// </summary>
        [HttpPost("{id}/reiniciar-password")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        public async Task<IActionResult> ReiniciarPassword(int id)
        {
            var ejecutorId = GetUserIdFromToken();
            var ejecutor = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == ejecutorId);
            var objetivo = await _context.Usuarios.Include(u => u.Rol).Include(u => u.Persona).FirstOrDefaultAsync(u => u.Id == id);

            if (objetivo == null || ejecutor?.Rol == null) return NotFound();
            if (ejecutor.Rol.Nombre != "SuperAdmin" && objetivo.Rol.Nivel >= ejecutor.Rol.Nivel) return Forbid();

            objetivo.PasswordHash = _passwordHasher.Hash(objetivo.Persona.DNI);
            objetivo.RequiereCambioPassword = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password reseteado." });
        }

        #endregion

        #region Perfil de Usuario (Self-Service)

        /// <summary>
        /// Permite al usuario autenticado cambiar su propia contraseña.
        /// </summary>
        /// <remarks>
        /// 💡 **Lógica de Seguridad:**
        /// - Valida que la contraseña actual sea correcta antes de aplicar el cambio.
        /// - Al tener éxito, desactiva automáticamente el flag 'RequiereCambioPassword'.
        /// </remarks>
        /// <param name="dto">Objeto con la contraseña actual, la nueva y su confirmación.</param>
        /// <response code="200">Contraseña actualizada con éxito.</response>
        /// <response code="400">Error de validación o contraseña actual incorrecta.</response>
        [HttpPost("cambiar-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarPassword([FromBody] ChangePasswordDto dto)
        {
            // 1. Identificamos al usuario desde el Token JWT
            var userId = GetUserIdFromToken();
            if (userId == 0) return Unauthorized(new { message = "Sesión no válida." });

            // 2. Buscamos al usuario en la base de datos de AWS RDS
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            // 🛡️ Validación previa para asegurar que no sea nulo ni vacío
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { message = "La nueva contraseña no puede estar vacía." });
            }
            if (usuario == null || dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "Error en la validación de datos." });
            // 3. Validaciones de negocio
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "La nueva contraseña y la confirmación no coinciden." });

            // 4. Verificación de identidad (Seguridad)
            if (!_passwordHasher.Verify(dto.CurrentPassword, usuario.PasswordHash))
                return BadRequest(new { message = "La contraseña actual es incorrecta." });

            // 5. Aplicación de cambios y limpieza de flags de primer acceso
            usuario.PasswordHash = _passwordHasher.Hash(dto.NewPassword!);
            usuario.RequiereCambioPassword = false; // ✅ Usuario ya está al día

            await _context.SaveChangesAsync();
            return Ok(new { message = "Tu contraseña ha sido actualizada exitosamente." });
        }

        /// <summary>
        /// Verifica si el usuario logueado tiene pendiente el cambio de contraseña obligatorio.
        /// </summary>
        [HttpGet("{id}/validar-primer-acceso")]
        public async Task<IActionResult> ValidarPrimerAcceso(int id)
        {
            var usuario = await _context.Usuarios
                .Select(u => new { u.Id, u.RequiereCambioPassword })
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return Ok(new { requiereCambio = usuario.RequiereCambioPassword });
        }

        #endregion

        [HttpPost("{id}/foto")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        public async Task<IActionResult> UploadFoto(int id, IFormFile archivo)
        {
            var usuario = await _context.Usuarios.Include(u => u.Persona).FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null || archivo == null || archivo.Length == 0) return BadRequest();

            var fileName = $"{usuario.Persona.DNI}_{DateTime.Now.Ticks}{Path.GetExtension(archivo.FileName)}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", fileName);

            using (var stream = new FileStream(path, FileMode.Create)) { await archivo.CopyToAsync(stream); }

            usuario.Persona.FotoPath = $"/uploads/profiles/{fileName}";
            await _context.SaveChangesAsync();
            return Ok(new { fotoUrl = usuario.Persona.FotoPath });
        }
        /// <summary>
        /// Elimina de forma permanente un usuario y su perfil asociado (Persona).
        /// Protege la jerarquía institucional y evita el auto-borrado.
        /// </summary>
        /// <response code="200">Eliminado con éxito.</response>
        /// <response code="403">Jerarquía insuficiente o intento de auto-borrado.</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.SecurityUserManage)]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var ejecutorId = GetUserIdFromToken();

            // 1. Regla de Oro: Un administrador no puede suicidarse digitalmente
            if (id == ejecutorId)
                return BadRequest(new { message = "No puedes eliminar tu propia cuenta desde este panel." });

            var ejecutor = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == ejecutorId);
            var objetivo = await _context.Usuarios.Include(u => u.Rol).Include(u => u.Persona).FirstOrDefaultAsync(u => u.Id == id);

            if (objetivo == null || ejecutor?.Rol == null) return NotFound();

            // 2. Validación de Jerarquía Institucional
            // Un Admin (Nivel 80) no puede borrar a otro Admin (Nivel 80) ni a un SuperAdmin (Nivel 100)
            if (ejecutor.Rol.Nombre != "SuperAdmin" && objetivo.Rol.Nivel >= ejecutor.Rol.Nivel)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "No tienes rango suficiente para eliminar a este usuario." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Eliminamos el Usuario (La cuenta de acceso)
                _context.Usuarios.Remove(objetivo);

                // 4. Eliminamos la Persona (El perfil físico: DNI, nombres, etc.)
                // Esto es importante para no dejar "datos huérfanos" en la tabla Personas
                if (objetivo.Persona != null)
                {
                    _context.Personas.Remove(objetivo.Persona);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Usuario y perfil eliminados permanentemente del sistema." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new
                {
                    message = "No se puede eliminar el usuario porque tiene registros asociados (ej. torneos, documentos). Prueba desactivándolo mejor.",
                    detail = ex.Message
                });
            }
        }

    }
}