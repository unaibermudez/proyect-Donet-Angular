# 04 · CRUD de productos: endpoints, validación y errores

## Qué hemos hecho

La API ya permite gestionar el catálogo completo en `/api/products`:

| Método | Ruta | Qué hace | Respuestas |
|---|---|---|---|
| `GET` | `/api/products` | Lista los productos ordenados por nombre. Filtro opcional `?category=Phone\|Computer\|Console` | 200 |
| `GET` | `/api/products/{id}` | Obtiene un producto | 200, 404 |
| `POST` | `/api/products` | Crea un producto | 201 + cabecera `Location`, 400 |
| `PUT` | `/api/products/{id}` | Reemplaza todos los datos de un producto | 200, 400, 404 |
| `DELETE` | `/api/products/{id}` | Borra un producto | 204, 404 |

Y alrededor de esos endpoints:

- **DTOs de entrada y salida** (`ProductRequest`, `ProductResponse`) en lugar de exponer la entidad.
- **Validación automática** con Data Annotations y la validación integrada de .NET 10. Una regla propia: la fecha de lanzamiento como mucho un año en el futuro, para permitir reservas.
- **Todos los errores en formato ProblemDetails**: validación, 404, JSON mal formado y excepciones inesperadas.
- **Logging estructurado**: logs propios con *message templates*, JSON en producción y texto compacto en desarrollo.
- **12 tests unitarios** de las reglas de validación.
- **6 tests de integración** de los endpoints contra un **Postgres real y desechable** con Testcontainers. En total, 21 tests contando los 3 del paso 2.
- Un archivo **`DocAssist.Api.http`** con todas las peticiones preparadas, incluidas las incorrectas.

Durante el paso se hizo una cosa a propósito: el `POST` se creó primero **sin validación** para ver qué se colaba. Un cuerpo vacío daba un **500** y unos datos absurdos (precio negativo, fecha de 2099) **se guardaban**. La validación del tramo 4 es la solución a ese fallo concreto.

## Conceptos nuevos

### DTOs y por qué no exponer la entidad

| Problema de exponer `Product` | Ejemplo |
|---|---|
| *Overposting* | El cliente manda `"id": 5` en un `POST` y pisa el que genera la base de datos |
| API acoplada al esquema | Renombrar una columna cambia el JSON y rompe el frontend |
| Entrada y salida son distintas | Al crear no hay `id`; al leer, sí |

- **`ProductResponse`** es un *record* posicional e inmutable, con un método `FromEntity` que mapea a mano.
- **`ProductRequest`** es un *record* con propiedades `init` (solo asignables al crear el objeto), porque lleva atributos de validación en cada propiedad. Tiene `ToEntity()` para el `POST` y `ApplyTo(product)` para el `PUT`.

El mapeo es **manual**, sin AutoMapper ni Mapster (los equivalentes de MapStruct): con un solo DTO, una librería esconde lo que pasa y sus errores aparecen en tiempo de ejecución.

### Organización por funcionalidad

```
Features/Products/
├── ProductEndpoints.cs
├── ProductRequest.cs
└── ProductResponse.cs
```

Todo lo de la API de productos vive junto, en vez de en carpetas `Controllers/`, `Dtos/`... (*vertical slices*). En Spring sería un paquete `products` con su controlador y sus DTOs. `Program.cs` solo lo enchufa con `app.MapProductEndpoints()`.

### `MapGroup` y métodos de extensión

```csharp
public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/api/products").WithTags("Products");
    group.MapGet("/", GetProducts);
    ...
}
```

- `MapGroup` aplica un prefijo común: es el `@RequestMapping("/api/products")` a nivel de clase.
- El `this` del primer parámetro hace que sea un **método de extensión**: se llama como `app.MapProductEndpoints()` aunque esté definido en otra clase. Java no tiene nada igual; Kotlin sí.
- Los endpoints se pasan como **métodos con nombre** (`GetProducts`) en lugar de lambdas: cada uno se lee como un método de un controlador.

### De dónde salen los parámetros de un endpoint

| Parámetro | Origen | En Spring |
|---|---|---|
| `int id` con `{id:int}` en la ruta | Ruta | `@PathVariable` |
| `ProductCategory? category` | *Query string* (opcional por ser anulable) | `@RequestParam(required = false)` |
| `ProductRequest request` | Cuerpo JSON | `@RequestBody` |
| `AppDbContext db`, `ILoggerFactory` | Contenedor de dependencias | Inyección por constructor |
| `CancellationToken` | La petición: se cancela si el cliente se desconecta | Sin equivalente directo en Spring MVC |

`{id:int}` es una **restricción de ruta**: `/api/products/abc` no coincide y da 404 sin llegar al método.

### `TypedResults` y `Results<...>`

```csharp
Task<Results<Ok<ProductResponse>, NotFound>> GetProductById(...)
```

`TypedResults.Ok(...)`, `NotFound()`, `Created(url, body)` y `NoContent()` son el `ResponseEntity` de Spring. El tipo `Results<Ok<...>, NotFound>` declara qué respuestas puede dar el endpoint: el compilador lo comprueba y OpenAPI lo documenta sin anotaciones extra.

### LINQ y EF Core

- Las consultas LINQ (`Where`, `OrderBy`, `Select`) **no se ejecutan** hasta `ToListAsync()` / `FirstOrDefaultAsync()`. Se van componiendo y EF Core las traduce a **un único** `SELECT`. Por eso el filtro por categoría es un simple `if`.
- **`AsNoTracking()`** en lecturas: EF Core no vigila cambios. Equivale a `@Transactional(readOnly = true)`.
- **`SaveChangesAsync()`** escribe todo lo pendiente en una transacción, como el *flush* + *commit* de Hibernate.
- **Change tracking** en el `PUT`: se carga con `FindAsync`, se cambian propiedades y `SaveChangesAsync()` genera un `UPDATE` **solo de las columnas que cambiaron**. Es el *dirty checking* de Hibernate.
- **`ExecuteDeleteAsync()`** en el `DELETE`: un único `DELETE ... WHERE` sin cargar la entidad, como `@Modifying @Query` en Spring Data.

### Validación (Data Annotations + .NET 10)

| .NET | Bean Validation |
|---|---|
| `[Required]` en `string` | `@NotBlank` |
| `[Required]` en `int?` / enum `?` | `@NotNull` |
| `[MaxLength(200)]` | `@Size(max = 200)` |
| `[Range(0.01, 100_000)]` | `@DecimalMin` + `@DecimalMax` |
| `IValidatableObject.Validate()` | Restricción a nivel de clase / `ConstraintValidator` |
| `builder.Services.AddValidation()` | `@Valid` en cada parámetro |

Desde **.NET 10**, `AddValidation()` valida automáticamente los parámetros de los endpoints que tengan atributos. Si algo falla, responde **400 con ProblemDetails** y el endpoint no llega a ejecutarse.

**Los campos obligatorios de tipo valor son anulables** (`ProductCategory?`, `decimal?`, `int?`, `DateOnly?`). Si fueran `ProductCategory` o `int`, un campo ausente llegaría como `Phone` o `0` sin que se notara; siendo anulables, llega `null` y `[Required]` lo rechaza. Es lo mismo que usar `Integer` en vez de `int` en un DTO de Java.

Detalles de C# que aparecen en la regla propia: `yield return` (devuelve errores uno a uno) y `nameof(ReleaseDate)` (el nombre de la propiedad comprobado por el compilador).

### ProblemDetails (RFC 9457)

Un formato estándar de error en JSON: `type`, `title`, `status`, `detail`, `instance` y, en .NET, `traceId`. Tres piezas en `Program.cs`:

| Pieza | Qué hace | En Spring |
|---|---|---|
| `AddProblemDetails(...)` | Registra el servicio que escribe ProblemDetails; aquí además añade `instance` = método + ruta | `spring.mvc.problemdetails.enabled=true` |
| `UseExceptionHandler(...)` | Excepción no controlada → error con ProblemDetails, sin detalles internos | `@ExceptionHandler(Exception.class)` en un `@ControllerAdvice` |
| `UseStatusCodePages()` | Respuestas de error con cuerpo vacío (como `NotFound()`) → ProblemDetails | Sin equivalente directo |

El `StatusCodeSelector` del gestor de excepciones usa el código que trae una `BadHttpRequestException` (400, o 413 si el cuerpo es demasiado grande) y reserva el 500 para el resto.

### El pipeline de middleware

`UseExceptionHandler` y `UseStatusCodePages` son **middleware**: piezas por las que pasa cada petición en orden, antes de llegar al endpoint, y la respuesta en orden inverso. Es la cadena de **filtros de Servlet**. El orden de registro es el orden de ejecución, por eso el gestor de excepciones va **el primero**: solo protege lo que viene después de él.

### `ThrowOnBadRequest`

En desarrollo, las Minimal APIs **lanzan una excepción** cuando no pueden leer la petición; en producción responden 400 directamente. Lo desactivamos también en desarrollo (`RouteHandlerOptions.ThrowOnBadRequest = false`) para que los dos entornos se comporten igual y un error del cliente no se registre como error del servidor.

### Logging estructurado

```csharp
logger.LogInformation("Product {ProductId} created: {ProductName} ({Category})",
    product.Id, product.Name, product.Category);
```

- Los huecos con **nombre** generan **campos separados** (`ProductId = 12`) además de la frase. En SLF4J sería `log.info("Product {} created", id)`, pero sin nombres.
- **Nunca interpolar** (`$"Product {id}"`): se pierden los campos. El compilador avisa con CA2254.
- **Categoría**: el nombre del logger. `ProductEndpoints` es `static` y no se puede usar en `ILogger<T>`, así que se pide a `ILoggerFactory` por nombre (`"DocAssist.Api.Products"`), como `LoggerFactory.getLogger("...")` en Java.
- **Formato**: JSON en producción (con `Scopes`: `TraceId`, `RequestPath`) y texto de una línea en desarrollo. Se configura en `appsettings.json`, como `logging.*` en `application.yml`.
- **Correlación**: el `traceId` del ProblemDetails que ve el cliente es el mismo que el de los logs de esa petición.

### Tests unitarios de la validación

- `Validator.TryValidateObject(..., validateAllProperties: true)` ejecuta las mismas reglas que la API. **Sin `validateAllProperties: true` solo se comprueba `[Required]`.**
- Cada caso parte de una **petición válida** y estropea **un único campo** con `with` (copia de un record cambiando propiedades, como el `toBuilder()` de Lombok). `Assert.Single` comprueba que hay exactamente ese error y ningún otro.
- `[Theory]` + `[MemberData]` + `TheoryData<...>` = `@ParameterizedTest` + `@MethodSource`.

### Tests de integración con Testcontainers

| Opción para probar contra una base de datos | Problema |
|---|---|
| La de `docker compose` (desarrollo) | Los tests ensucian los datos y fallan si no está levantada |
| EF Core InMemory o SQLite | No es Postgres: otro SQL, otros tipos. La documentación de EF Core lo desaconseja |
| **Testcontainers** ✅ | Un Postgres real que se crea al empezar y se destruye al acabar |

Es la misma librería que en Java (`@Testcontainers` + `@Container`). En .NET se monta como un *fixture* de xUnit:

```csharp
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("pgvector/pgvector:pg17").Build();
    ...
}
```

- **`WebApplicationFactory<Program>`**, la del paso 2, arranca la API en memoria.
- **`IAsyncLifetime`** es el `@BeforeAll` / `@AfterAll` asíncrono de xUnit. `InitializeAsync` arranca el contenedor y aplica las **migraciones reales** con `MigrateAsync()`, que además ejecuta el *seed* de desarrollo. `DisposeAsync` lo destruye.
- **`ConfigureAppConfiguration` + `AddInMemoryCollection`** sustituye la cadena de conexión por la del contenedor. Al añadirse la última, tiene prioridad sobre `appsettings.Development.json`. Es el `@DynamicPropertySource` de Spring.
- **`IClassFixture<PostgresApiFactory>`**: un contenedor por clase de tests, compartido entre sus tests.
- Se usa la **misma imagen** que en `docker-compose.yml`, para que los tests vean el mismo Postgres, con pgvector, que la aplicación.

**Reglas para que los tests sean fiables:**

- **Cada test crea sus propios datos** y comprueba solo esos: no cuenta cuántos productos hay ni depende del *seed* ni del orden de ejecución.
- **Se comprueba también lo que no debe pasar**: el filtro por categoría verifica que el móvil creado **no** aparece entre las consolas, no solo que las consolas sí.
- **El cliente de los tests habla el mismo JSON que la API**: `JsonStringEnumConverter` en sus opciones, porque la API envía y recibe los enums como texto.

**Detalle de C#: implementación explícita de interfaz.** `WebApplicationFactory` ya tiene un `DisposeAsync()` que devuelve `ValueTask`, e `IAsyncLifetime` de xUnit pide uno que devuelva `Task`. Como no pueden convivir dos métodos con el mismo nombre y distinto tipo de retorno, el de xUnit se implementa como `async Task IAsyncLifetime.DisposeAsync()`: solo se usa cuando se llama a través de la interfaz. Java no tiene este mecanismo.

## Archivos importantes

| Archivo | Qué hace |
|---|---|
| `Features/Products/ProductEndpoints.cs` | Grupo `/api/products` con los cinco endpoints y los logs de escritura |
| `Features/Products/ProductRequest.cs` | DTO de entrada con las reglas de validación, `ToEntity()` y `ApplyTo()` |
| `Features/Products/ProductResponse.cs` | DTO de salida y su mapeo desde la entidad |
| `Program.cs` | Enums como texto en JSON, `ThrowOnBadRequest = false`, `AddValidation()`, ProblemDetails, gestor de excepciones, `UseStatusCodePages()` y `MapProductEndpoints()` |
| `appsettings.json` | Niveles de log de producción y salida en JSON |
| `appsettings.Development.json` | Niveles de log de desarrollo (SQL de EF Core visible) y salida en texto compacto |
| `DocAssist.Api.http` | Peticiones de ejemplo, correctas e incorrectas, para probar la API desde el editor |
| `tests/.../Features/Products/ProductRequestValidationTests.cs` | 12 tests unitarios de las reglas de validación |
| `tests/.../Infrastructure/PostgresApiFactory.cs` | Arranca la API en memoria contra un Postgres de Testcontainers y aplica las migraciones |
| `tests/.../Features/Products/ProductEndpointsTests.cs` | 6 tests de integración: crear y leer, validación, 404, modificar, borrar y filtrar |
| `tests/DocAssist.Api.Tests.csproj` | Añade `Testcontainers.PostgreSql` 4.15.0 |

## Cómo probarlo

Con Docker arrancado y la base de datos migrada (paso 3):

```powershell
cd C:\dev\PERSONAL\proyecto-dotnet-angular\backend
dotnet run --project src/DocAssist.Api
```

**Desde el editor:** abrir `src/DocAssist.Api/DocAssist.Api.http` y lanzar las peticiones (VS Code con la extensión *REST Client*, Rider o Visual Studio). También desde **http://localhost:5080/scalar**.

**Desde el terminal:**

```powershell
curl http://localhost:5080/api/products
curl "http://localhost:5080/api/products?category=Console"
curl http://localhost:5080/api/products/1
curl -i http://localhost:5080/api/products/999        # 404 con ProblemDetails
curl -i http://localhost:5080/api/nada                # 404 con ProblemDetails
```

**Qué comprobar:**

| Petición del `.http` | Resultado esperado |
|---|---|
| Crear un producto | 201 con cabecera `Location` |
| Modificar | 200; en el log, un `UPDATE` solo de las columnas cambiadas |
| Borrar | 204; la segunda vez, 404 |
| Cuerpo vacío `{}` | 400 con los 7 campos obligatorios en `errors` |
| Datos absurdos | 400 con un error por cada campo |
| Categoría `"Tablet"` | 400 |
| JSON mal formado | 400 |
| Con la base de datos parada (`docker compose stop db`) | 500 genérico, sin traza; la excepción completa en el log con el mismo `traceId` |

**Ver los logs en formato de producción (JSON):**

```powershell
$env:Logging__Console__FormatterName = "json"
$env:Logging__Console__FormatterOptions__IncludeScopes = "true"
dotnet run --project src/DocAssist.Api
# ...probar...
Remove-Item Env:Logging__Console__FormatterName
Remove-Item Env:Logging__Console__FormatterOptions__IncludeScopes
```

**Tests** (necesitan **Docker Desktop arrancado** para los de integración):

```powershell
dotnet test                                           # 21 tests
dotnet test --logger "console;verbosity=normal"       # con el nombre y la duración de cada test
dotnet test --filter "FullyQualifiedName~ProductEndpointsTests"   # solo los de integración
```

La primera ejecución tarda algo más: Testcontainers descarga su contenedor auxiliar (Ryuk), que se encarga de borrar los contenedores aunque los tests se interrumpan.

**Comprobar que los tests no tocan la base de desarrollo:** después de ejecutarlos, en la base de `docker compose` no debe haber ningún producto de marca `Valve`, que es la que usan los tests:

```powershell
docker compose exec db psql -U docassist -d docassist -c "SELECT count(*) FROM products WHERE brand = 'Valve';"   # 0
```

Esta comprobación importa: si la sustitución de la cadena de conexión fallara, los tests usarían la base de desarrollo, que también está levantada, y **pasarían igual** mientras la ensucian.

## Decisiones y alternativas

| Decisión | Por qué | Alternativas |
|---|---|---|
| DTOs separados de la entidad | Evita *overposting* y desacopla el JSON del esquema | Exponer `Product` directamente |
| Mapeo manual | Explícito y comprobado por el compilador | AutoMapper, Mapster |
| Un solo `ProductRequest` para `POST` y `PUT` | Los dos reciben exactamente los mismos campos | Un DTO por operación, útil si divergen |
| `PUT` reemplaza todo; sin `PATCH` | El formulario del frontend siempre enviará el producto completo | `PATCH` con JSON Patch o un DTO de campos opcionales |
| Validación integrada de .NET 10 con Data Annotations | Viene con el framework, es declarativa y devuelve ProblemDetails sin código extra | FluentValidation: reglas en código, más expresiva para reglas complejas, una dependencia más |
| Campos obligatorios de tipo valor anulables | Distingue "no enviado" de "enviado como 0" | Tipos no anulables, que aceptan valores por defecto en silencio |
| Mensajes de validación en inglés | La API la consumen desarrolladores; los mensajes para el usuario los pondrá el frontend | Mensajes en español con `ErrorMessage = "..."` en cada atributo |
| `DbContext` en los endpoints, sin servicios ni repositorios | La lógica es CRUD sin reglas de negocio; otra capa solo reenviaría llamadas | Un `ProductService`, que tendría sentido con lógica de negocio real |
| Sin paginación en el listado | El catálogo es pequeño (10 productos) | `?page=&pageSize=` con `Skip`/`Take` |
| ProblemDetails para todos los errores | Un solo formato que el frontend sabe interpretar | Formatos de error propios |
| `UseExceptionHandler` con `StatusCodeSelector` | Errores de lectura de la petición → 400; resto → 500 | Gestionar cada excepción con un `IExceptionHandler` propio |
| `ThrowOnBadRequest = false` también en desarrollo | Mismo comportamiento que producción y sin errores falsos en el log | Dejar el valor por defecto, que lanza excepciones en desarrollo |
| `ILoggerFactory` con categoría por nombre | La clase de endpoints es `static` y no se puede usar en `ILogger<T>` | `ILogger<Program>`, con una categoría poco descriptiva; o el generador `[LoggerMessage]`, más rápido pero más código |
| Solo se registran las escrituras | Las lecturas ya las registra ASP.NET Core; más logs serían ruido | Registrar cada lectura |
| JSON en producción, texto en desarrollo | JSON para los sistemas de logs; texto para leer en la consola | Serilog, con más *sinks* y enriquecedores, como dependencia externa |
| Archivo `.http` versionado | Documentación viva de la API, ejecutable desde el editor | Colecciones de Postman, fuera del repositorio |
| Tests de integración con Testcontainers | Postgres real, mismo SQL y mismos tipos que en producción; desechable | EF Core InMemory o SQLite (no son Postgres); la base de desarrollo (se ensucia) |
| Misma imagen `pgvector/pgvector:pg17` que Docker Compose | Los tests prueban exactamente el Postgres de la aplicación, con pgvector para el paso 8 | `postgres` oficial, sin pgvector |
| Un contenedor por clase de tests (`IClassFixture`) | Arrancarlo cuesta unos segundos; por test sería muy lento | Uno por test (aislamiento total, mucho más lento) o uno para todo el proyecto (`ICollectionFixture`) |
| Cada test crea sus propios datos | Independientes del *seed* y del orden de ejecución | Limpiar la base entre tests (por ejemplo, con la librería Respawn) |
| Los tests usan los DTOs de la API | El DTO tiene 11 campos; copiarlo duplicaría mucho código. El objetivo aquí es el comportamiento, no el contrato | Copias locales del contrato, como el `AppInfo` del paso 2, que detectarían renombrados de propiedades |

## Para la entrevista

**Frases que puedo decir:**

> "Los endpoints están agrupados por funcionalidad con `MapGroup`, y cada uno declara sus posibles respuestas con `TypedResults`, así que OpenAPI las documenta solo. Uso DTOs separados de la entidad para evitar *overposting* y no acoplar el JSON al esquema de la base de datos."

> "La validación usa la integrada de .NET 10 con Data Annotations. Primero creé el `POST` sin validar para ver qué se colaba: un cuerpo vacío daba 500 y unos datos absurdos se guardaban. Un detalle importante: los campos obligatorios de tipo valor son anulables, porque si no, un enum ausente llegaría como su primer valor sin que nadie se enterara."

> "Todos los errores salen en ProblemDetails con un `traceId`, y los logs son estructurados en JSON con el mismo `traceId`. Si un usuario me manda el error que ve, encuentro en los logs todo lo que pasó en esa petición."

**Posibles preguntas:**

- *¿Por qué `AsNoTracking` en las lecturas y no en el `PUT`?*
  En lecturas, el seguimiento de cambios es trabajo inútil. En el `PUT` lo necesito: cargo la entidad, la modifico y `SaveChanges` genera el `UPDATE` solo de lo que cambió.

- *¿Qué diferencia hay entre `Results` y `TypedResults`?*
  `Results.Ok()` devuelve un `IResult` genérico. `TypedResults.Ok()` devuelve un tipo concreto (`Ok<T>`), que el compilador comprueba y OpenAPI usa para documentar la respuesta.

- *¿Por qué no se ve el mensaje de la excepción en un 500?*
  Porque revela detalles internos (base de datos, librerías, rutas) que no le sirven al cliente y sí a un atacante. El cliente recibe un `traceId`, y la excepción completa está en el log con ese mismo id.

- *¿Qué es un message template y por qué no usar interpolación?*
  Es una plantilla con huecos con nombre. El logger guarda cada valor como un campo aparte, que luego se puede filtrar. Con `$"..."` el texto se forma antes de llegar al logger y los campos se pierden.

- *¿Data Annotations o FluentValidation?*
  Para reglas sencillas por campo, Data Annotations: vienen con el framework y desde .NET 10 se aplican solas en Minimal APIs. FluentValidation compensa con reglas complejas o condicionales, o cuando se quiere separar las reglas del DTO.

- *¿Por qué el orden de los middleware importa?*
  Cada uno envuelve a los siguientes, como los filtros de Servlet. El gestor de excepciones tiene que ir el primero para capturar las excepciones de todo lo que viene después.

- *¿Por qué Testcontainers y no una base de datos en memoria?*
  Porque InMemory o SQLite no son Postgres: traducen el SQL de otra forma y no tienen los mismos tipos ni restricciones, así que un test puede pasar y el código fallar en producción. Con Testcontainers pruebo contra la misma imagen que uso en Docker Compose, con las migraciones reales.

- *¿Cómo sabes que los tests de integración no usan tu base de desarrollo?*
  La cadena de conexión del contenedor se añade la última, así que tiene prioridad. Y lo comprobé: después de ejecutar los tests, en la base de desarrollo no hay ninguno de los productos que crean.
