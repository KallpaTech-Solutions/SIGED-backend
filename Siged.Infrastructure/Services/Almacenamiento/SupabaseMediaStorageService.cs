using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Siged.Application.Interfaces.Almacenamiento;
using System.Net.Http.Headers;

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

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            // Usamos el bucketName que viene por parámetro
            var uploadUrl = $"{baseUrl}/storage/v1/object/{bucketName}/{fileName}";

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Add("Authorization", $"Bearer {key}");
            request.Headers.Add("apikey", key);

            using var stream = file.OpenReadStream();
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            request.Content = content;

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                return $"{baseUrl}/storage/v1/object/public/{bucketName}/{fileName}";
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Supabase rechazó la subida al bucket '{bucketName}': {errorBody}");
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