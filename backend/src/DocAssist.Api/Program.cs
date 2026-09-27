using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Registro de servicios (antes de Build) ----------

// Genera el documento OpenAPI a partir de los endpoints definidos.
builder.Services.AddOpenApi();

// Comprobaciones de salud. En el paso 3 añadiremos una que verifique la base de datos.
builder.Services.AddHealthChecks();

var app = builder.Build();

// ---------- 2. Pipeline y endpoints (después de Build) ----------

// La documentación de la API solo se expone en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // JSON en /openapi/v1.json
    app.MapScalarApiReference();    // interfaz web en /scalar
}

app.MapHealthChecks("/health");

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
