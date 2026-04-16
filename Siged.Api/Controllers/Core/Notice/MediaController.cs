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
        public async Task<IActionResult> UploadNoticia([FromForm] IFormFileCollection files, CancellationToken ct)
        {
            // Usar IFormFileCollection con [FromForm] es la forma más "limpia" 
            // para que .NET mapee automáticamente el multipart/form-data.

            if (files == null || files.Count == 0)
            {
                // Si el binder falla, intentamos leerlo manualmente de la request como último recurso
                files = (IFormFileCollection)Request.Form.Files;
            }

            if (files.Count == 0)
                return BadRequest("No se recibieron archivos en el campo 'files'.");

            var urls = await _mediaService.UploadFilesAsync(files, "noticias", ct);
            return Ok(new { urls });
        }
    }
}