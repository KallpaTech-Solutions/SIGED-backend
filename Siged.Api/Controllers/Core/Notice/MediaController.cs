using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Domain.Entities.Security;

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
        /// Sube archivos (Fotos/Video) y devuelve las URLs de Supabase.
        /// </summary>
        [HttpPost("upload-noticia")]
        [Authorize(Policy = Permissions.NewsCreate)]
        [RequestSizeLimit(52_428_800)] // 🔥 Aumentado a 50MB para soportar video
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)] // Necesario para IIS/Kestrel
        public async Task<IActionResult> UploadNoticia([FromForm(Name = "files")] IEnumerable<IFormFile> files, CancellationToken ct)
        {
            if (files == null || !files.Any())
                return BadRequest("No se recibieron archivos o el nombre del campo no es 'files'.");

            try
            {
                // El servicio ya maneja la subida a Supabase
                var urls = await _mediaService.UploadNoticiasAsync(files, ct);

                if (urls == null || !urls.Any())
                    return StatusCode(500, "La subida falló en el servidor de almacenamiento.");

                return Ok(new { urls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}