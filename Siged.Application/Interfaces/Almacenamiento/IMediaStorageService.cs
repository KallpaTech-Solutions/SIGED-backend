using Microsoft.AspNetCore.Http;

namespace Siged.Application.Interfaces.Almacenamiento
{
    public interface IMediaStorageService
    {
        /// <summary>
        /// Sube un archivo individual a un bucket específico.
        /// </summary>
        Task<string> UploadFileAsync(IFormFile file, string bucketName, CancellationToken ct = default);

        /// <summary>
        /// Sube múltiples archivos y devuelve la lista de URLs.
        /// </summary>
        Task<IReadOnlyList<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string bucketName, CancellationToken ct = default);
    }
}