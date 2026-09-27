using DocAssist.Api.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Registro de servicios (antes de Build) ----------

// Genera el documento OpenAPI a partir de los endpoints definidos.
builder.Services.AddOpenApi();

// Base de datos: PostgreSQL con nombres de tablas y columnas en snake_case.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention();

    // Datos de ejemplo solo en desarrollo. Se insertan al aplicar las migraciones.
    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSeeding((context, _) => SampleData.Seed(context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                SampleData.SeedAsync(context, cancellationToken));
    }
});

// La comprobación de la base de datos lleva la etiqueta "ready" para que
// solo la ejecute /health/ready (ver abajo).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: ["ready"]);

var app = builder.Build();

// ---------- 2. Pipeline y endpoints (después de Build) ----------

// La documentación de la API solo se expone en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // JSON en /openapi/v1.json
    app.MapScalarApiReference();    // interfaz web en /scalar
}

// Liveness: ¿el proceso está vivo? No comprueba dependencias, así que una caída
// de la base de datos no hace que se reinicie la aplicación.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: ¿puede atender peticiones? Comprueba la base de datos.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapGet("/api/info", (IConfiguration config, IHostEnvironment env) =>
        new AppInfoResponse(
            Name: config["App:Name"] ?? "DocAssist",
            Environment: env.EnvironmentName,
            DotnetVersion: Environment.Version.ToString()))
    .WithTags("System")
    .WithSummary("Información básica de la aplicación");

app.Run();

// ---------- Tipos ----------

record AppInfoResponse(string Name, string Environment, string DotnetVersion);
