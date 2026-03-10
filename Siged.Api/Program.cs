using Siged.Api.Extensions;
using Siged.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICIOS MODULARIZADOS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Aplicamos nuestras extensiones
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerCustom(); // <-- Aquí ya tienes configurado el SecurityDefinition
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);

// Health Checks
var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHealthChecks().AddNpgSql(conn);

var app = builder.Build();

// --- PIPELINE DE MIDDLEWARE ---
app.UseCors("AllowReactApp");

// ✅ Swagger habilitado para TODOS los entornos (incluyendo Render)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SIGED - API UNAS V1");
    c.RoutePrefix = string.Empty; // ✨ Esto hace que Swagger cargue en la raíz (https://siged-backend.onrender.com/)
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();