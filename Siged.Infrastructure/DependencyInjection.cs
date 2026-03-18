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
            // 1. Configuración de la Base de Datos (PostgreSQL) con Split Query
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // 🚀 Divide las consultas con muchos 'Include' en pequeñas partes rápidas.
                    // Esto evita el Timeout de 32 segundos en el puerto 6543.
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                    // Aumentamos el tiempo de espera a 60s por la latencia Huánuco -> Ohio
                    npgsqlOptions.CommandTimeout(60);
                }));

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