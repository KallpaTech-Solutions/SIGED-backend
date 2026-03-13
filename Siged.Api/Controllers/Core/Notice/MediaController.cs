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
        [RequestSizeLimit(52_428_800)] // 50MB en bytes
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
        public async Task<IActionResult> UploadNoticia(CancellationToken ct)
        {
            // Leemos directamente de los archivos adjuntos en el Form
            var files = Request.Form.Files;

            if (files == null || files.Count == 0)
            {
                return BadRequest(new
                {
                    message = "No se recibieron archivos. Asegúrate de que el FormData use la clave 'files'."
                });
            }

            try
            {
                // El servicio procesa la subida a Supabase
                var urls = await _mediaService.UploadNoticiasAsync(files, ct);

                if (urls == null || !urls.Any())
                {
                    return StatusCode(500, new { message = "Error al procesar la subida a Supabase." });
                }

                return Ok(new { urls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", details = ex.Message });
            }
        }
    }
}