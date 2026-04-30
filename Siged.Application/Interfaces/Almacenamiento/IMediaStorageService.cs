using Microsoft.AspNetCore.Http;

namespace Siged.Application.Interfaces.Almacenamiento
{
    public interface IMediaStorageService
    {
        /// <summary>
        /// Sube un archivo a Supabase Storage. <paramref name="bucketName"/> es el nombre del bucket,
        /// o "bucket/prefijo/carpeta" para guardar bajo una ruta dentro del bucket (p. ej. acta-logos/default-left).
        /// </summary>
        Task<string> UploadFileAsync(IFormFile file, string bucketName, CancellationToken ct = default);

        /// <summary>
        /// Sube múltiples archivos y devuelve la lista de URLs.
        /// </summary>
        Task<IReadOnlyList<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string bucketName, CancellationToken ct = default);
    }
}