using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Siged.Api.Extensions;
using Siged.Api.Hubs;
using Siged.Api.Services;
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
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<IMediaStorageService, SupabaseMediaStorageService>();
builder.Services.AddScoped<FixtureService>();
builder.Services.AddScoped<PlayoffService>();
builder.Services.AddScoped<CompetitionFormatSetupService>();
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Para que clockPeriodAnchorUtc: null llegue al cliente al pausar (limpiar ancla).
        options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });
builder.Services.AddScoped<MatchSportRulesBuilder>();
builder.Services.AddScoped<StandingsService>();
builder.Services.AddScoped<TournamentManagerService>();
builder.Services.AddScoped<DisciplineRuleService>();
builder.Services.AddScoped<BracketService>();
builder.Services.AddSingleton<TournamentVitrinaBroadcastService>();
builder.Services.AddSingleton<ZonaHorariaPublicStateStore>();
builder.Services.AddSingleton<MatchBroadcastWidgetStore>();

// Extensiones de arquitectura y seguridad
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerCustom();
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);

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

// Swagger: UI en /swagger y /swagger/index.html; el spec JSON en /swagger/v1/swagger.json
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SIGED - API UNAS V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }