using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Siged.Infrastructure.Services.Security;
using Siged.Infrastructure.Persistence;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Security;
using Siged.Application.Interfaces.Security;
using Siged.Domain.Entities.Core;

namespace Siged.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<JwtProvider> _jwtProviderMock; 


        public AuthServiceTests()
        {
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _jwtProviderMock = new Mock<JwtProvider>(null!);
        }

        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task LoginAsync_DebeRetornarNull_CuandoUsuarioNoExiste()
        {
            var db = GetDbContext();
            var service = new AuthService(db, _passwordHasherMock.Object, _jwtProviderMock.Object);

            var result = await service.LoginAsync("usuario_inexistente", "password123");

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_DebeLanzarExcepcion_CuandoUsuarioEstaInactivo()
        {
            var db = GetDbContext();
            var usuario = new Usuario
            {
                Username = "inactivo",
                EstaActivo = false,
                PasswordHash = "any_hash",
                Persona = new Administrador { DNI = "12345678", Nombres = "Test", Apellidos = "User" },
                Rol = new Rol { Nombre = "Admin" }
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            var service = new AuthService(db, _passwordHasherMock.Object, _jwtProviderMock.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync("inactivo", "password123"));
        }

        [Fact]
        public async Task LoginAsync_DebeRetornarNull_CuandoPasswordEsIncorrecto()
        {
            var db = GetDbContext();
            var usuario = new Usuario
            {
                Username = "gregorio",
                EstaActivo = true,
                PasswordHash = "hash_real",
                Persona = new Administrador { DNI = "12345678", Nombres = "Gregorio", Apellidos = "Paz" },
                Rol = new Rol { Nombre = "Admin" }
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            _passwordHasherMock.Setup(x => x.Verify("password_mal", "hash_real")).Returns(false);

            var service = new AuthService(db, _passwordHasherMock.Object, _jwtProviderMock.Object);

            var result = await service.LoginAsync("gregorio", "password_mal");

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_DebeRetornarDto_CuandoCredencialesSonValidas()
        {
            var db = GetDbContext();
            var usuario = new Usuario
            {
                Username = "benjamin",
                EstaActivo = true,
                PasswordHash = "hash_ok",
                Persona = new Administrador { DNI = "12345678", Nombres = "Benjamin", Apellidos = "Paz" },
                Rol = new Rol { Nombre = "SuperAdmin" }
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            _passwordHasherMock.Setup(x => x.Verify("password123", "hash_ok")).Returns(true);
            _jwtProviderMock.Setup(x => x.Generate(usuario)).Returns("token_generado_xyz");

            var service = new AuthService(db, _passwordHasherMock.Object, _jwtProviderMock.Object);

            var result = await service.LoginAsync("benjamin", "password123");

            result.Should().NotBeNull();
            result!.Token.Should().Be("token_generado_xyz");
            result.Username.Should().Be("benjamin");
            result.Rol.Should().Be("SuperAdmin");
        }
    }
}