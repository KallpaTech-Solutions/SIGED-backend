using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Extensions;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Infrastructure;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Almacenamiento;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<IMediaStorageService, SupabaseMediaStorageService>();

// Extensiones de arquitectura y seguridad
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerCustom();
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);

// Health Checks para monitoreo en Render
var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHealthChecks().AddNpgSql(conn);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// --- 2. BLOQUE DE AUTO-MIGRACIÓN Y SEEDING ---
// Este bloque asegura que Supabase tenga las tablas y datos al arrancar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Aplica migraciones pendientes y ejecuta el modelBuilder.Seed()
        if (context.Database.GetPendingMigrations().Any())
        {
            Console.WriteLine("⏳ Aplicando migraciones y seeding en Supabase...");
            context.Database.Migrate();
            Console.WriteLine("✅ Base de datos actualizada con éxito.");
        }
        else
        {
            Console.WriteLine("ℹ️ La base de datos ya está al día.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Ocurrió un error al migrar la base de datos.");
    }
}

// PIPELINE DE MIDDLEWARE
app.UseForwardedHeaders();
app.UseCors("AllowReactApp");

// Swagger habilitado en la raíz para facilitar pruebas
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SIGED - API UNAS V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program { }