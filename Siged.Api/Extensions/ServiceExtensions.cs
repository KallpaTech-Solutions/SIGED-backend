using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Security;
using System.Text;

namespace Siged.Api.Extensions;

public static class ServiceExtensions
{
    // 1. Configuración de Swagger
    public static IServiceCollection AddSwaggerCustom(this IServiceCollection services)
    {
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SIGED - API UNAS",
                Version = "v1",
                Description = "Gestión de eventos deportivos"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pega tu token JWT aquí."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] { } }
            });
        });
        return services;
    }

    // 2. Seguridad: Autenticación + Autorización Dinámica
    public static IServiceCollection AddSecurityConfiguration(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["JwtSettings:Issuer"],
                ValidAudience = config["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Secret"]!))
            };

            // 🔥 NUEVO: Evento para validar la "Lista Negra"
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var tokenRaw = (context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken)?.RawData;

                    // ¿Está este token en la lista negra?
                    var esInvalido = await db.TokensInvalidados.AnyAsync(t => t.Token == tokenRaw);

                    if (esInvalido)
                    {
                        context.Fail("Este token ha sido revocado (Logout).");
                    }
                }
            };
        });
        // JWT
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["JwtSettings:Issuer"],
                    ValidAudience = config["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Secret"]!))
                };
            });

        // Handler y Políticas dinámicas (Unificadas)
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionHandler>();
        services.AddAuthorization(options => {
            foreach (var permission in Permissions.GetAllNames())
            {
                options.AddPolicy(permission, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }

    // 3. CORS
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration config)
    {
        services.AddCors(options => {
            options.AddPolicy("AllowReactApp", policy => {
                var origins = config.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                var devOrigins = new[] { "https://localhost:5173", "http://localhost:5173" };
                policy.WithOrigins(origins.Concat(devOrigins).ToArray())
                      .AllowAnyMethod().AllowAnyHeader().AllowCredentials();
            });
        });
        return services;
    }

    
}