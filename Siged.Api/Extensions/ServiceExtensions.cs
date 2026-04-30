using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Siged.Api.Authorization;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Security;
using System.Linq;
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
            // Evita colisión de nombres cortos entre DTOs (Swagger 500) y documenta multipart.
            c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
            c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
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
        // Scoped: usa ApplicationDbContext (no puede ser Singleton).
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, TournDelegateOrTeamGestorHandler>();
        services.AddAuthorization(options => {
            foreach (var permission in Permissions.GetAllNames())
            {
                options.AddPolicy(permission, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }

            // Inscripción de equipos y gestión de plantel: admin OTI o delegado (tourn.team.manage)
            options.AddPolicy(TournDelegateAuth.PolicyName, policy =>
                policy.RequireAssertion(ctx =>
                {
                    var p = ctx.User.FindAll("permission").Select(c => c.Value).ToHashSet();
                    return p.Contains(Permissions.TournManage) || p.Contains(Permissions.TournTeamManage);
                }));

            // Misma área funcional, pero también usuarios con fila en TeamGestores (co-delegados).
            options.AddPolicy(TournDelegateOrTeamGestorAuth.PolicyName, policy =>
                policy.Requirements.Add(new TournDelegateOrTeamGestorRequirement()));

            options.AddPolicy(TournFormatSetupAuth.PolicyName, policy =>
                policy.RequireAssertion(ctx => TournFormatSetupAuth.CanSetupFormat(ctx.User)));

            // Detalle de partido para mesa: control de acta o gestión de torneo (evita 403 a Admin OTI sin match.control).
            options.AddPolicy("tourn.mesa.detail", policy =>
                policy.RequireAssertion(ctx =>
                {
                    var p = ctx.User.FindAll("permission").Select(c => c.Value).ToHashSet();
                    return p.Contains(Permissions.TournMatchControl)
                        || p.Contains(Permissions.TournManage)
                        || p.Contains(Permissions.TournTeamManage);
                }));

            options.AddPolicy(Permissions.TournMesaBroadcast, policy =>
                policy.RequireAssertion(ctx =>
                {
                    var p = ctx.User.FindAll("permission").Select(c => c.Value).ToHashSet();
                    return p.Contains(Permissions.TournMatchControl) || p.Contains(Permissions.TournMatchWidgets);
                }));
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
                      .AllowCredentials()
                      .SetIsOriginAllowedToAllowWildcardSubdomains();
            });
        });
        return services;
    }


}