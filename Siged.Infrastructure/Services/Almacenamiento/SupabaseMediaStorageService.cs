using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Siged.Application.Interfaces.Almacenamiento;
using System.Net.Http.Headers;
using System.Net.Http;

namespace Siged.Infrastructure.Services.Almacenamiento
{
    public class SupabaseMediaStorageService : IMediaStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public SupabaseMediaStorageService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // ✅ Implementación del método para UN solo archivo
        public async Task<string> UploadFileAsync(IFormFile file, string bucketName, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0) return string.Empty;

            var baseUrl = _config["Supabase:Url"]?.Trim().TrimEnd('/');
            var key = _config["Supabase:ServiceKey"]?.Trim();

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(key))
                throw new Exception("Configuración de Supabase incompleta.");

            // Supabase: primer segmento = bucket; el resto = prefijo de objeto dentro del bucket.
            // Ej.: "acta-logos/default-left" → bucket "acta-logos", clave "default-left/{guid}_archivo.png"
            var slash = bucketName.IndexOf('/');
            string bucket;
            string objectKey;
            if (slash < 0)
            {
                bucket = bucketName;
                objectKey = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            }
            else
            {
                bucket = bucketName[..slash];
                var prefix = bucketName[(slash + 1)..].Trim('/');
                objectKey = string.IsNullOrEmpty(prefix)
                    ? $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}"
                    : $"{prefix}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            }

            var uploadUrl = $"{baseUrl}/storage/v1/object/{bucket}/{objectKey}";

            // Copiar a RAM antes del POST: con varios IFormFile en multipart, abrir el segundo stream
            // puede fallar o dejar el primero vacío si el body no está totalmente en buffer.
            byte[] payload;
            await using (var read = file.OpenReadStream())
            {
                using var buf = new MemoryStream();
                await read.CopyToAsync(buf, ct);
                payload = buf.ToArray();
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("Authorization", $"Bearer {key}");
            request.Headers.Add("apikey", key);

            using var content = new ByteArrayContent(payload);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            request.Content = content;

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                return $"{baseUrl}/storage/v1/object/public/{bucket}/{objectKey}";
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Supabase rechazó la subida al bucket '{bucket}' (objeto '{objectKey}'): {errorBody}");
        }

        // ✅ Implementación del método para MÚLTIPLES archivos
        public async Task<IReadOnlyList<string>> UploadFilesAsync(IEnumerable<IFormFile> files, string bucketName, CancellationToken ct = default)
        {
            var urls = new List<string>();

            foreach (var file in files)
            {
                var url = await UploadFileAsync(file, bucketName, ct);
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add(url);
                }
            }

            return urls.AsReadOnly();
        }
    }
}