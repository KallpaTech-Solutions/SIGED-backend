using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Security;
using Microsoft.AspNetCore.Http;

namespace Siged.Api.Controllers.Core.Notice
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaStorageService _mediaService;

        public MediaController(IMediaStorageService mediaService)
        {
            _mediaService = mediaService;
        }

        /// <summary>
        /// Sube archivos multimedia a Supabase (Máx. 50MB).
        /// </summary>
        [HttpPost("upload-noticia")]
        [Authorize(Policy = Permissions.NewsCreate)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadNoticia(CancellationToken ct)
        {
            try
            {
                // 1. Verificamos si es un formulario válido
                if (!Request.HasFormContentType)
                    return BadRequest("La petición no es un formulario válido (multipart/form-data).");

                var files = Request.Form.Files;

                if (files == null || files.Count == 0)
                    return BadRequest("No se recibieron archivos en el campo 'files'.");

                // 2. Llamada al servicio
                var urls = await _mediaService.UploadNoticiasAsync(files, ct);

                return Ok(new { urls });
            }
            catch (Exception ex)
            {
                // Esto nos permitirá ver el error real en los logs de Render
                Console.WriteLine($"🔥 ERROR CRÍTICO EN MEDIA: {ex.Message}");
                Console.WriteLine($"🔥 STACKTRACE: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error interno", detail = ex.Message });
            }
        }
    }
}