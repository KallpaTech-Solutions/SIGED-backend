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

                // 🔥 Único bloque de eventos para validar la "Lista Negra"
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var tokenRaw = (context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken)?.RawData;

                        // Validación de seguridad para evitar nulidad
                        if (!string.IsNullOrEmpty(tokenRaw))
                        {
                            var esInvalido = await db.TokensInvalidados.AnyAsync(t => t.Token == tokenRaw);
                            if (esInvalido)
                            {
                                context.Fail("Este token ha sido revocado (Logout).");
                            }
                        }
                    }
                };
            });

        // Handler y Políticas dinámicas (Esto se mantiene igual)
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
                // Intentamos leer la lista, pero si falla, usamos un fallback
                var originsFromConfig = config.GetSection("AllowedOrigins").Get<string[]>();

                var allowedList = new List<string> {
                "https://siged-unas.tech",
                "https://www.siged-unas.tech",
                "https://localhost:5173",
                "http://localhost:5173"
            };

                if (originsFromConfig != null)
                {
                    allowedList.AddRange(originsFromConfig);
                }

                policy.WithOrigins(allowedList.Distinct().ToArray()) // Evita duplicados
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });
        return services;
    }


}