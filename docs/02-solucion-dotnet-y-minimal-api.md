# 02 · Solución .NET y primera Minimal API

## Qué hemos hecho

Hemos creado el backend desde cero, con dos proyectos agrupados en una solución:

```
backend/
├── DocAssist.slnx                        ← la solución
├── src/
│   └── DocAssist.Api/                    ← la API
│       ├── DocAssist.Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Properties/launchSettings.json
└── tests/
    └── DocAssist.Api.Tests/              ← los tests
        ├── DocAssist.Api.Tests.csproj
        └── SystemEndpointsTests.cs
```

La API tiene tres cosas:

- **`GET /health`**: comprobación de salud, responde `Healthy`.
- **`GET /api/info`**: devuelve el nombre de la aplicación (leído de configuración), el entorno y la versión de .NET.
- **Documentación OpenAPI** en `/openapi/v1.json` con interfaz web en `/scalar`, **solo en desarrollo**.

Y tres tests de integración que arrancan la API en memoria y comprueban los dos endpoints y que la documentación **no** se expone fuera de desarrollo.

## Conceptos nuevos

### Solución y proyecto

Un **proyecto** (`.csproj`) es un módulo: tiene su código, sus dependencias y produce un resultado (un ejecutable o una librería). Una **solución** (`.slnx`) es solo una lista de proyectos, para compilarlos y testearlos juntos.

| .NET | Maven |
|---|---|
| `DocAssist.slnx` | `pom.xml` padre con `<modules>` |
| `DocAssist.Api.csproj` | `pom.xml` de un módulo |
| `<PackageReference>` | `<dependency>` de Maven Central |
| `<ProjectReference>` | `<dependency>` a otro módulo del mismo proyecto |

A diferencia de Maven, **los tests son un proyecto aparte**, no una carpeta `src/test` dentro del mismo módulo. El proyecto de tests referencia al de la API con `<ProjectReference>`. Así el código de test nunca acaba en el ejecutable.

`.slnx` es el formato nuevo de .NET 10: XML legible. El formato antiguo `.sln` estaba lleno de GUIDs y nadie lo editaba a mano.

### El CLI `dotnet`

| Comando | Qué hace | Equivalente |
|---|---|---|
| `dotnet new <plantilla>` | Genera un proyecto o archivo desde una plantilla | `spring init`, `npm create vite` |
| `dotnet sln add` | Añade un proyecto a la solución | Añadir un `<module>` al pom padre |
| `dotnet add package` | Añade un paquete NuGet al `.csproj` | Añadir una `<dependency>` |
| `dotnet add reference` | Añade una dependencia a otro proyecto de la solución | Dependencia entre módulos |
| `dotnet build` | Compila | `mvn compile` |
| `dotnet run` | Compila y ejecuta | `mvn spring-boot:run` |
| `dotnet test` | Compila y ejecuta los tests | `mvn test` |

Si ejecutas `build` o `test` en la carpeta del `.slnx`, actúan sobre **todos** los proyectos.

### El `.csproj` de una API

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- `Sdk="Microsoft.NET.Sdk.Web"` incluye ASP.NET Core y el servidor Kestrel. No hay que añadirlos como dependencia: vienen con el SDK. Equivale a `spring-boot-starter-web`.
- `Nullable enable` hace que el compilador distinga entre `string` (nunca nulo) y `string?` (puede ser nulo) y te avise si usas un valor que puede ser nulo sin comprobarlo. Java no tiene nada equivalente integrado en el compilador.
- `ImplicitUsings enable` importa automáticamente los namespaces más usados.

### `Program.cs` y sus dos mitades

```csharp
var builder = WebApplication.CreateBuilder(args);
// 1. Registrar servicios: builder.Services.Add...()
var app = builder.Build();
// 2. Configurar middleware y endpoints: app.Map...(), app.Use...()
app.Run();
```

- **Antes de `Build()`** se registran servicios en el contenedor de inyección de dependencias. Equivale a declarar los `@Bean`.
- **Después de `Build()`** se montan el pipeline de middleware y los endpoints. Ya no se pueden registrar servicios.

No hay clase ni método `main`: son *top-level statements*, y el compilador genera la clase `Program` por nosotros.

### Minimal API

Un endpoint se define con una ruta y una función, sin clase controlador:

```csharp
app.MapGet("/api/info", (IConfiguration config, IHostEnvironment env) => ...);
```

| Minimal API | Spring MVC |
|---|---|
| `app.MapGet("/ruta", ...)` | `@GetMapping("/ruta")` en un método de un `@RestController` |
| Parámetros de la lambda con servicios | Inyección por constructor en el controlador |

**Los parámetros de la lambda se inyectan solos.** ASP.NET Core mira el tipo de cada parámetro: si es un servicio registrado, lo saca del contenedor; si no, intenta sacarlo de la ruta, la *query string* o el cuerpo de la petición.

La alternativa en .NET son los **controladores** (`[ApiController]`), muy parecidos a los de Spring. Minimal APIs es el enfoque moderno y el que pide la oferta.

### Configuración por capas

| .NET | Spring Boot |
|---|---|
| `appsettings.json` | `application.yml` |
| `appsettings.Development.json` | `application-dev.yml` |
| `ASPNETCORE_ENVIRONMENT=Development` | `SPRING_PROFILES_ACTIVE=dev` |
| `config["App:Name"]` | `@Value("${app.name}")` |
| Variable `App__Name` | Variable `APP_NAME` |

El orden de precedencia, de menor a mayor: `appsettings.json` → `appsettings.{Entorno}.json` → variables de entorno → argumentos de línea de comandos. El último que define un valor gana.

### `launchSettings.json`

Configura cómo arranca la app con `dotnet run` o desde el IDE: puertos, entorno, si abre el navegador. Es una *Run Configuration* de IntelliJ guardada en el repo. **Solo afecta a tu máquina**; en Docker o producción no se lee.

### Health checks

`AddHealthChecks()` + `MapHealthChecks("/health")` es el equivalente a `/actuator/health`. Ahora responde siempre `Healthy`. En el paso 3 añadiremos una comprobación de la base de datos.

### OpenAPI y Scalar

| .NET 9+ | Spring Boot |
|---|---|
| `Microsoft.AspNetCore.OpenApi` → `/openapi/v1.json` | `springdoc-openapi` → `/v3/api-docs` |
| `Scalar.AspNetCore` → `/scalar` | Swagger UI → `/swagger-ui.html` |

Hasta .NET 8 las plantillas usaban *Swashbuckle*, que traía el documento y Swagger UI juntos. Desde .NET 9, Microsoft genera el documento con su propio paquete y no incluye interfaz: cada proyecto elige la suya.

### `record`

```csharp
record AppInfoResponse(string Name, string Environment, string DotnetVersion);
```

Una clase inmutable en una línea: constructor, propiedades, igualdad por valor y `ToString` generados. Es igual que un `record` de Java 16+. Ideal para DTOs.

### xUnit y `WebApplicationFactory`

| xUnit | JUnit 5 / Spring |
|---|---|
| `[Fact]` | `@Test` |
| `[Theory]` + `[InlineData]` | `@ParameterizedTest` + `@ValueSource` |
| `Assert.Equal(esperado, real)` | `assertEquals(esperado, real)` |
| `IClassFixture<T>` | Contexto compartido entre los tests de una clase |
| `WebApplicationFactory<Program>` | `@SpringBootTest` + `MockMvc` |
| `WithWebHostBuilder(b => b.UseEnvironment("Production"))` | `@ActiveProfiles("prod")` |

`WebApplicationFactory` arranca la API completa **en memoria**: sin abrir puertos, con peticiones que no salen del proceso. Es rápido y no choca con la API que tengas corriendo.

### `async` / `await` y `Task`

`Task<T>` es el equivalente a `CompletableFuture<T>`. `await` espera el resultado sin bloquear el hilo, pero el código se lee como si fuera secuencial, sin encadenar `.thenApply()`. En .NET casi todo lo que toca red, disco o base de datos es asíncrono.

### Constructor primario

```csharp
public class SystemEndpointsTests(WebApplicationFactory<Program> factory) { ... }
```

Los parámetros entre paréntesis después del nombre de la clase son un constructor, y se pueden usar en cualquier método de la clase. Ahorra escribir el constructor y el campo, como hace Lombok con `@RequiredArgsConstructor`.

## Archivos importantes

| Archivo | Qué hace |
|---|---|
| `backend/DocAssist.slnx` | Lista los dos proyectos, organizados en carpetas virtuales `/src/` y `/tests/` |
| `src/DocAssist.Api/DocAssist.Api.csproj` | Proyecto web para `net10.0`, con los paquetes `Microsoft.AspNetCore.OpenApi` 10.0.12 y `Scalar.AspNetCore` 2.17.10 |
| `src/DocAssist.Api/Program.cs` | Registra OpenAPI y health checks; expone `/health`, `/api/info` y, solo en desarrollo, `/openapi/v1.json` y `/scalar` |
| `src/DocAssist.Api/appsettings.json` | Configuración base: nombre de la app (`App:Name`) y niveles de log |
| `src/DocAssist.Api/appsettings.Development.json` | Configuración que se aplica encima cuando el entorno es `Development` |
| `src/DocAssist.Api/Properties/launchSettings.json` | Perfil único `http` en el puerto **5080**, entorno `Development`, sin abrir el navegador |
| `tests/DocAssist.Api.Tests/DocAssist.Api.Tests.csproj` | Proyecto de tests: xUnit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing` y referencia a la API |
| `tests/DocAssist.Api.Tests/SystemEndpointsTests.cs` | Tres tests de integración de `/health`, `/api/info` y de que OpenAPI no se expone en `Production` |

## Cómo probarlo

Todo desde `backend/`:

```powershell
cd C:\dev\PERSONAL\proyecto-dotnet-angular\backend

# Compilar la solución completa
dotnet build

# Ejecutar los tests (3 deben pasar)
dotnet test

# Arrancar la API
dotnet run --project src/DocAssist.Api
```

Con la API corriendo, en otro terminal:

```powershell
curl http://localhost:5080/health        # Healthy
curl http://localhost:5080/api/info      # {"name":"DocAssist","environment":"Development","dotnetVersion":"10.0.x"}
```

Y en el navegador: http://localhost:5080/scalar

**Ver la precedencia de la configuración:**

```powershell
$env:App__Name = "Nombre desde variable de entorno"
dotnet run --project src/DocAssist.Api
# /api/info devuelve ahora el nombre de la variable
Remove-Item Env:App__Name
```

**Recarga en caliente** (el equivalente a `spring-boot-devtools`): con `dotnet watch` la API se recompila y reinicia sola al guardar un archivo.

```powershell
dotnet watch --project src/DocAssist.Api
```

## Decisiones y alternativas

| Decisión | Por qué | Alternativas |
|---|---|---|
| Plantilla `web` (vacía) | Cada línea del proyecto la hemos puesto sabiendo por qué | `webapi`: trae un ejemplo `WeatherForecast` que habría que borrar |
| Minimal APIs | Es lo que pide la oferta y el enfoque moderno de .NET. Menos ceremonia: ruta y lógica juntas | Controladores con `[ApiController]`: más parecidos a Spring MVC, más código repetitivo |
| Tests en un proyecto separado | Es la convención de .NET y evita que el código de test acabe en el ejecutable | No hay alternativa razonable en .NET |
| Solo HTTP en local, puerto 5080 fijo | Sin certificados de desarrollo que instalar. En producción el HTTPS lo termina el proxy inverso | Mantener el perfil `https` con `dotnet dev-certs https --trust` |
| OpenAPI solo en desarrollo | No exponer el mapa completo de la API en producción | Exponerlo siempre, o protegerlo con autenticación |
| Scalar como interfaz | La opción más extendida desde que .NET dejó de incluir Swagger UI; interfaz moderna | Swashbuckle (Swagger UI), NSwag |
| Test de integración ya en el paso 2 | Confirma que toda la cadena SDK → compilación → tests funciona antes de añadir complejidad | Esperar al paso 4, cuando haya endpoints de negocio |
| `record` propio en el test en vez de reutilizar el de la API | El test ve el JSON como lo vería un cliente y detecta cambios de contrato | Reutilizar `AppInfoResponse` (haría falta hacerlo público) |

## Para la entrevista

**Frases que puedo decir:**

> "El backend es una solución con dos proyectos: la API con Minimal APIs y un proyecto de tests con xUnit. Desde el principio tengo tests de integración con `WebApplicationFactory`, que arranca la API entera en memoria, así que pruebo el pipeline real y no solo métodos sueltos."

> "La documentación OpenAPI solo se expone en desarrollo, y tengo un test que lo verifica: arranca la app en entorno `Production` y comprueba que `/openapi/v1.json` da 404. Es una regla de seguridad, y si alguien la rompe, falla la build."

> "Vengo de Spring Boot, así que lo que más me ayudó fue mapear conceptos: `Program.cs` tiene dos fases, registrar servicios antes de `Build()` y configurar el pipeline después, que es como declarar beans y luego tener el contexto listo."

**Posibles preguntas:**

- *¿Qué diferencia hay entre Minimal APIs y controladores?*
  Los controladores agrupan endpoints en clases con atributos, como en Spring MVC. Minimal APIs define cada endpoint con una ruta y una función, con menos código y algo más de rendimiento. Para organizarlas se agrupan por funcionalidad con `MapGroup`, que es lo que haremos en el paso 4.

- *¿Cómo se inyectan dependencias en una Minimal API?*
  Como parámetros de la función del endpoint. El framework ve el tipo y, si es un servicio registrado, lo resuelve del contenedor. No hace falta constructor.

- *¿Qué hace `WebApplicationFactory`?*
  Arranca la aplicación completa en memoria, con su configuración y servicios reales, y da un `HttpClient` que la llama sin pasar por la red. Permite además sustituir servicios o cambiar el entorno para un test concreto.

- *¿Por qué no hay Swagger?*
  Desde .NET 9 Microsoft genera el documento OpenAPI con `Microsoft.AspNetCore.OpenApi` y ya no incluye Swashbuckle en las plantillas. La interfaz se elige aparte; yo uso Scalar.

- *¿En qué orden se aplica la configuración?*
  `appsettings.json`, luego `appsettings.{Entorno}.json`, luego variables de entorno y luego argumentos de línea de comandos. El último gana. Por eso los secretos van en variables de entorno y nunca en el JSON versionado.

- *¿Qué es `Nullable enable`?*
  Activa los tipos de referencia anulables: el compilador distingue `string` de `string?` y avisa si usas algo que puede ser nulo sin comprobarlo. Evita muchos `NullReferenceException` en tiempo de compilación.
