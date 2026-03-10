using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Siged.Application.Interfaces.Security;
using Siged.Application.Services.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Security;
using Siged.Infrastructure.Services.Security;

namespace Siged.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Configuración de la Base de Datos (PostgreSQL)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // 2. Registro de tus Servicios de Seguridad (RBAC)
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<JwtProvider>();
            // Aquí irás agregando los nuevos servicios 
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            
            return services;
        }
    }
}