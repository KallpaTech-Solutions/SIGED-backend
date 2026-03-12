using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siged.Application.Interfaces;
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
        /// Sube archivos al equipo y devuelve las URLs de Supabase.
        /// </summary>
        [HttpPost("upload-noticia")]
        [Authorize(Policy = Permissions.NewsCreate)]
        [RequestSizeLimit(20_000_000)] // 20MB
        public async Task<IActionResult> UploadNoticia([FromForm] List<IFormFile> files, CancellationToken ct)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No se enviaron archivos.");

            // 🔥 Aquí es donde se usa el servicio que creaste
            var urls = await _mediaService.UploadNoticiasAsync(files, ct);

            return Ok(new { urls });
        }
    }
}
