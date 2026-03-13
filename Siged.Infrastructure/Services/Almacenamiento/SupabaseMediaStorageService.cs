using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Siged.Application.Interfaces.Almacenamiento;
using System;
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

        public async Task<IReadOnlyList<string>> UploadNoticiasAsync(IEnumerable<IFormFile> files, CancellationToken ct = default)
        {
            var urls = new List<string>();

            // Leemos las variables que pusiste en Render
            var baseUrl = _config["Supabase:Url"];
            if (string.IsNullOrEmpty(baseUrl)) throw new Exception("¡LA URL DE SUPABASE NO ESTÁ LLEGANDO AL CÓDIGO!");
            var key = _config["Supabase:ServiceKey"];
            var bucket = _config["Supabase:BucketNoticias"];

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Generar nombre único para evitar colisiones
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

                // La URL de destino en la API de Supabase
                var uploadUrl = $"{baseUrl}/storage/v1/object/{bucket}/{fileName}";

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);

                // Usamos la ServiceKey (llave maestra) para tener permiso total
                request.Headers.Add("Authorization", $"Bearer {key}");
                request.Headers.Add("apikey", key);

                using var stream = file.OpenReadStream();
                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                request.Content = content;

                var response = await _httpClient.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    // Como el bucket es público, construimos la URL de acceso directo
                    var publicUrl = $"{baseUrl}/storage/v1/object/public/{bucket}/{fileName}";
                    urls.Add(publicUrl);
                }
            }

            return urls.AsReadOnly();
        }
    }
}
