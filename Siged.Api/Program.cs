using Siged.Api.Extensions; // Importamos nuestras extensiones
using Siged.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICIOS MODULARIZADOS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Aplicamos nuestras extensiones
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerCustom();
builder.Services.AddSecurityConfiguration(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);

// Health Checks
var conn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHealthChecks().AddNpgSql(conn);

var app = builder.Build();

// --- PIPELINE DE MIDDLEWARE ---
app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();