
using Microsoft.AspNetCore.Http;

namespace Siged.Application.Interfaces.Almacenamiento
{
    public interface IMediaStorageService
    {
        /// <summary>
        /// Sube una lista de archivos al storage y devuelve sus URLs públicas.
        /// </summary>
        /// <param name="files">Lista de archivos recibidos por el controlador.</param>
        /// <param name="ct">Token de cancelación.</param>
        Task<IReadOnlyList<string>> UploadNoticiasAsync(IEnumerable<IFormFile> files, CancellationToken ct = default);
    }
}
