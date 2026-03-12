using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Siged.Application.Interfaces.Security;
using Siged.Domain.Entities.Core;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Siged.UnitTests.Integration
{
    public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public AuthIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_ConBenjamin_DebeRetornarToken()
        {
            // 1. ARRANGE: Preparamos al usuario en la DB de memoria
            await SeedUser(_factory);
            var loginDto = new { Username = "admin_benjamin", Password = "76063362" };

            // 2. ACT: Enviamos la petición de login
            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginDto);

            // 3. ASSERT: Verificamos respuesta exitosa
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            result.Should().ContainKey("token");
            result!["token"].ToString().Should().NotBeNullOrEmpty();
        }


        // --- MÉTODO AUXILIAR PARA LLENAR LA DB DE PRUEBAS ---
        private async Task SeedUser(WebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Solo agregamos si no existe ya en este fixture
            if (!await db.Usuarios.AnyAsync(u => u.Username == "admin_benjamin"))
            {
                var admin = new Usuario
                {
                    Username = "admin_benjamin",
                    PasswordHash = hasher.Hash("76063362"),
                    EstaActivo = true,
                    Persona = new Administrador
                    {
                        DNI = "76063362",
                        Nombres = "Benjamin",
                        Apellidos = "Admin",
                        Correo = "admin@unas.edu.pe"
                    },
                    Rol = new Rol { Nombre = "SuperAdmin", Nivel = 100 }
                };
                db.Usuarios.Add(admin);
                await db.SaveChangesAsync();
            }
        }
    }
}