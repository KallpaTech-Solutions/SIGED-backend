using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Siged.Application.Interfaces.Almacenamiento;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

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

            // 1. Leemos y limpiamos las variables (Trim elimina espacios invisibles)
            var baseUrl = _config["Supabase:Url"]?.Trim().TrimEnd('/');
            var key = _config["Supabase:ServiceKey"]?.Trim();
            var bucket = _config["Supabase:BucketNoticias"]?.Trim();

            if (string.IsNullOrEmpty(baseUrl)) throw new Exception("🔥 ERROR: 'Supabase:Url' está vacío. Revisa Render.");
            if (string.IsNullOrEmpty(key)) throw new Exception("🔥 ERROR: 'Supabase:ServiceKey' está vacío.");
            if (string.IsNullOrEmpty(bucket)) throw new Exception("🔥 ERROR: 'Supabase:BucketNoticias' está vacío.");

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

                // 2. Construcción segura de la URL
                var uploadUrl = $"{baseUrl}/storage/v1/object/{bucket}/{fileName}";

                // LOG DE DIAGNÓSTICO: Veremos esto en Render para confirmar la ruta
                Console.WriteLine($"🚀 Intentando subir archivo a: {uploadUrl}");

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);

                // 3. Headers de autenticación
                request.Headers.Add("Authorization", $"Bearer {key}");
                request.Headers.Add("apikey", key);

                using var stream = file.OpenReadStream();
                var content = new StreamContent(stream);

                // IMPORTANTE: Definir el tipo de contenido (image/jpeg, video/mp4, etc.)
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                request.Content = content;

                try
                {
                    var response = await _httpClient.SendAsync(request, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        var publicUrl = $"{baseUrl}/storage/v1/object/public/{bucket}/{fileName}";
                        urls.Add(publicUrl);
                        Console.WriteLine($"✅ Subida exitosa: {fileName}");
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Error de Supabase ({response.StatusCode}): {errorBody}");
                        throw new Exception($"Supabase rechazó la subida: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"🔥 Error de conexión TCP: {ex.Message}");
                    throw;
                }
            }

            return urls.AsReadOnly();
        }
    }
}