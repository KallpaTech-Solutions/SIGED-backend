using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Extensions;
using Siged.Api.Hubs;
using Siged.Application.Interfaces.Almacenamiento;
using Siged.Infrastructure;
using Siged.Infrastructure.Persistence;
using Siged.Infrastructure.Services.Almacenamiento;
using Siged.Infrastructure.Services.Tournment;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<IMediaStorageService, SupabaseMediaStorageService>();
builder.Services.AddScoped<FixtureService>();
builder.Services.AddScoped<PlayoffService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<StandingsService>();
builder.Services.AddScoped<TournamentManagerService>();
builder.Services.AddScoped<DisciplineRuleService>();
builder.Services.AddScoped<BracketService>();

// Extensiones de arquitectura y seguridad
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerCustom();
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);


// --- DEBUG: Añade esto temporalmente ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"🔍 CADENA ACTIVA: {connectionString}");
// ---------------------------------------
// Health Checks para monitoreo en Render
var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHealthChecks().AddNpgSql(conn);
// Configurar límites de formulario para permitir archivos grandes (50MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800; // 50 MB
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// --- 2. BLOQUE DE AUTO-MIGRACIÓN Y SEEDING ---
// Este bloque asegura que Supabase tenga las tablas y datos al arrancar
/*using (var scope = app.Services.CreateScope())
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
*/
// PIPELINE DE MIDDLEWARE
app.UseForwardedHeaders();
app.UseRouting();
app.UseCors("AllowReactApp");
app.MapHub<TournamentHub>("/tournamentHub");

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