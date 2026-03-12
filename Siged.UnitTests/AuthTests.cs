using Xunit;
using Siged.Domain.Entities.Security;
using System;

namespace Siged.UnitTests
{
    public class AuthTests
    {
        [Fact]
        public void AuditoriaLog_DebeGuardarDatosCorrectamente()
        {
            // 1. Arrange (Preparar el escenario)
            var fechaActual = DateTime.UtcNow;
            var log = new AuditoriaLog
            {
                UsuarioId = 1,
                Accion = "LOGOUT",
                Detalle = "Cierre de sesión de prueba",
                Fecha = fechaActual,
                IpAddress = "190.235.1.1"
            };

            // 2. Act (No hay acción compleja aquí, solo lectura)

            // 3. Assert (Verificar que los datos coincidan)
            Assert.Equal(1, log.UsuarioId);
            Assert.Equal("LOGOUT", log.Accion);
            Assert.Equal("190.235.1.1", log.IpAddress);
            Assert.Equal(fechaActual, log.Fecha);
        }

        [Fact]
        public void TokenInvalidado_DebeTenerFechaExpiracionValida()
        {
            // Arrange
            var expira = DateTime.UtcNow.AddHours(2);
            var tokenInvalido = new TokenInvalidado
            {
                Token = "abc.123.xyz",
                FechaExpiracion = expira
            };

            // Assert
            Assert.True(tokenInvalido.FechaExpiracion > DateTime.UtcNow);
            Assert.NotEmpty(tokenInvalido.Token);
        }
    }
}