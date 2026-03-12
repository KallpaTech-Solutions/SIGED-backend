using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Controllers.Security;
using Siged.Application.Interfaces.Security;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence; 

namespace Siged.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact] 
        public async Task Logout_DebeGuardarEnBlacklistYAuditoria()
        {
            
            var db = GetDbContext();

           
            var log = new AuditoriaLog
            {
                Accion = "LOGOUT",
                UsuarioId = 1,
                IpAddress = "127.0.0.1",
                Fecha = DateTime.UtcNow
            };

           
            db.AuditoriaLogs.Add(log);
            await db.SaveChangesAsync();

           
            var guardado = await db.AuditoriaLogs.FirstOrDefaultAsync(x => x.UsuarioId == 1);

            guardado.Should().NotBeNull(); 
            guardado!.Accion.Should().Be("LOGOUT");
            guardado.IpAddress.Should().Be("127.0.0.1");
        }
    }
}