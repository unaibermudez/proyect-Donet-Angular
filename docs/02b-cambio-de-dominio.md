# 02b · Cambio de dominio: tienda de tecnología

## Qué hemos hecho

Hemos cambiado **de qué trata la aplicación**, no cómo está construida. Hasta ahora
era un asistente de documentación técnica para maquinaria de una fábrica. A partir
de aquí es un **asistente de tienda de tecnología** que vende productos de tres
categorías: **móviles, ordenadores y consolas**.

La arquitectura, el stack y la forma de trabajar son los mismos. Lo que cambia es
el contenido:

- La entidad principal pasa a ser **`Product`** (nombre, marca, modelo, categoría,
  precio, stock, fecha de lanzamiento y tres especificaciones opcionales: RAM,
  almacenamiento y pulgadas de pantalla).
- La categoría es un enum con tres valores: `Phone`, `Computer`, `Console`.
- El RAG responderá con **documentos de la tienda**: manuales de producto y
  documentos generales (garantía, devoluciones, envíos).
- El agente del paso 11 tendrá la herramienta **`query_products`** para consultar
  el catálogo, por ejemplo: *"¿qué móviles tenemos por debajo de 500 € con al
  menos 8 GB de RAM?"*.

El cambio se ha hecho a mitad del paso 3: la entidad y su configuración de EF Core
ya estaban escritas, pero **todavía no había migraciones ni datos**. Por eso ha
bastado con sustituir los archivos y actualizar la documentación.

## Conceptos nuevos

### Datos estructurados y datos no estructurados

Es la idea que justifica que la aplicación tenga **dos caminos** para responder.

**Datos estructurados** son los que caben en columnas con un tipo concreto: precio,
stock, RAM, almacenamiento, pantalla, categoría. Viven en tablas de PostgreSQL y se
consultan con SQL. Una pregunta como *"el móvil más barato con al menos 8 GB de
RAM"* necesita **filtrar, ordenar y comparar números exactos**. Los embeddings no
sirven para eso: miden parecido de significado, no saben que 499 € es menor que
500 €. Por eso el agente consultará estos datos con una herramienta
(`query_products`) que ejecuta una consulta SQL con filtros.

**Datos no estructurados** son textos libres: manuales, política de garantía, de
devoluciones, de envíos. No tienen columnas; la respuesta está en algún párrafo.
Se trocean en fragmentos (*chunks*), se convierten en embeddings, se guardan en
pgvector y se buscan por similitud: eso es el **RAG**.

| | Estructurados | No estructurados |
|---|---|---|
| Ejemplos | Precio, stock, RAM, almacenamiento, pantalla, categoría | Manuales, garantía, devoluciones, envíos |
| Dónde se guardan | Columnas de la tabla `products` | Fragmentos de texto + embeddings en pgvector |
| Cómo se consultan | SQL: `WHERE`, `ORDER BY`, `COUNT`... | Búsqueda por similitud (distancia coseno) |
| Tipo de respuesta | Exacta y verificable | La más parecida en significado |
| Herramienta del agente | `query_products` | `search_documentation` |
| Pregunta de ejemplo | *"¿Cuántas consolas tenemos en stock?"* | *"¿Cuántos días tengo para devolver un producto?"* |

Y hay preguntas **mixtas** que necesitan los dos caminos:

> *"¿Qué garantía tiene el móvil más barato con 256 GB?"*

Primero hay que averiguar **qué móvil es** (SQL: filtrar por categoría y
almacenamiento, ordenar por precio) y después **qué dice la garantía** (RAG sobre
el manual del producto o la política general). Un chat con RAG no lo resuelve
bien; un **agente** que decide qué herramienta llamar y en qué orden, sí. Esta es
la motivación del paso 11.

*En el mundo Spring:* sería como tener un `ProductRepository` con consultas JPA y,
al lado, un buscador de texto tipo Elasticsearch. Nadie usaría Elasticsearch para
calcular "el más barato", ni SQL para encontrar "el párrafo que habla de
devoluciones".

### Tipos de valor anulables: `int?` y `decimal?`

En C#, `int` y `decimal` son **tipos de valor**: nunca pueden ser `null` (un `int`
sin asignar vale `0`). Para permitir "sin valor" se escribe `int?`, que es una
abreviatura de `Nullable<int>`.

```csharp
public int? RamGb { get; set; }          // puede ser null
public decimal? ScreenInches { get; set; }
```

| C# | Java |
|---|---|
| `int` (nunca nulo) | `int` primitivo |
| `int?` (puede ser nulo) | `Integer` (wrapper, puede ser `null`) |
| `ram.HasValue`, `ram.Value`, `ram ?? 0` | `ram != null`, `ram`, `Optional.ofNullable(ram).orElse(0)` |

La diferencia con Java es que en C# no hay *autoboxing* escondido: el `?` está en
el tipo y el compilador obliga a tratar el caso nulo antes de usar el valor.
`Optional` de Java se parece más en intención, pero allí se recomienda solo para
valores de retorno; en C# `int?` se usa también en propiedades y parámetros.

**Cómo lo mapea EF Core:** por convención, una propiedad `int` genera una columna
`NOT NULL` y una `int?` genera una columna **que admite `NULL`**. No hace falta
configurar nada. Con `string` pasa lo mismo gracias a `<Nullable>enable</Nullable>`:
`string` → `NOT NULL`, `string?` → admite nulos. Es el equivalente a
`@Column(nullable = false)` de JPA, pero deducido del tipo.

Las tres especificaciones son anulables porque **no aplican igual a todos los
productos**, ni siquiera dentro de una misma categoría: una PS5 no tiene
pantalla, pero una Nintendo Switch sí. Guardar `0` pulgadas para la PS5 sería
mentir (el agente podría acabar diciendo que "tiene una pantalla de 0
pulgadas"). Por eso tampoco se puede decidir por categoría si un campo aplica:
lo decide cada producto.

### Por qué cambiar de dominio ahora sale barato

Cambiar el dominio cuesta más cuanto más cosas dependen de él. Ahora mismo:

- **No hay migraciones**: la base de datos aún no tiene ninguna tabla nuestra.
- **No hay datos**: nada que convertir ni que perder.
- **No hay endpoints, DTOs ni frontend** que usen la entidad.
- El `AppDbContext` todavía no está registrado en `Program.cs`.

Así que el cambio se reduce a sustituir cuatro archivos de código y actualizar la
documentación. Dentro de dos pasos habría supuesto tocar endpoints, tests,
componentes de Angular y datos.

### Migraciones: borrar y regenerar en desarrollo, nunca en producción

Una migración **no describe cómo es el esquema**, describe **una transformación**
de un esquema que ya existe ("añade la columna X", "renombra la tabla Y"). EF Core
apunta en la tabla `__EFMigrationsHistory` qué migraciones se han aplicado ya en
cada base de datos, igual que Flyway con `flyway_schema_history`.

- **En desarrollo temprano** se pueden borrar las migraciones y generar una nueva
  desde cero: la base de datos es desechable (se tira el contenedor y se vuelve a
  crear) y nadie más la tiene aplicada.
- **En producción, no.** Allí las migraciones ya están aplicadas y hay datos
  reales. Si borras una migración, la historia de la base deja de cuadrar con el
  código. Lo correcto es añadir **una migración nueva** que transforme lo que hay:
  renombrar la tabla, añadir columnas, convertir datos. Igual que en Flyway nunca
  se edita un `V3__...sql` ya aplicado, sino que se añade un `V4`.

> En este proyecto **no había ninguna migración que borrar**: el cambio se ha hecho
> antes de crear la primera. La primera migración se generará ya con `Product`.

## Archivos importantes

| Archivo | Estado | Qué hace |
|---|---|---|
| `backend/src/DocAssist.Api/Domain/Product.cs` | Creado | Entidad `Product` con las especificaciones opcionales |
| `backend/src/DocAssist.Api/Domain/ProductCategory.cs` | Creado | Enum `Phone`, `Computer`, `Console` |
| `backend/src/DocAssist.Api/Data/Configurations/ProductConfiguration.cs` | Creado | Longitudes máximas, categoría como texto, precisión de `Price` y `ScreenInches`, índice por categoría |
| `backend/src/DocAssist.Api/Data/AppDbContext.cs` | Modificado | `DbSet<Product> Products` en lugar del anterior |
| `backend/src/DocAssist.Api/Domain/Equipment.cs` | Borrado | Entidad del dominio anterior |
| `backend/src/DocAssist.Api/Domain/EquipmentCategory.cs` | Borrado | Enum del dominio anterior |
| `backend/src/DocAssist.Api/Data/Configurations/EquipmentConfiguration.cs` | Borrado | Configuración del dominio anterior |
| `README.md` | Modificado | Título, descripción, pregunta de ejemplo, diagrama e índice de documentación |
| `docs/00-plan-y-progreso.md` | Modificado | Pasos reescritos para el nuevo dominio y paso 3 marcado como en curso |
| `docs/01-entorno-y-estructura.md` | Modificado | Frase de la entrevista adaptada al nuevo dominio |
| `docs/02b-cambio-de-dominio.md` | Creado | Este documento |

Los archivos de `backend/` forman parte del paso 3, que todavía está en curso, y
entrarán en su commit. En el commit del cambio de dominio solo va la documentación.

## Cómo probarlo

Desde `backend/`:

```powershell
cd C:\dev\PERSONAL\proyecto-dotnet-angular\backend

# Compilar: debe terminar con 0 advertencias y 0 errores
dotnet build

# Tests: los 3 tests de integración deben seguir pasando
dotnet test
```

Comprobar que no queda ninguna referencia al dominio anterior (desde la raíz del
repositorio, en Git Bash):

```bash
cd "/c/dev/PERSONAL/proyecto-dotnet-angular"
grep -rniE --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj \
  "equipment|inversor|inverter|convertidor|frequencyconverter|powerkw|registeredon|query_equipment" .
```

Lo esperable es que solo aparezcan coincidencias en este mismo documento (que
explica el cambio y nombra los archivos borrados) y en `AI_REVIEW.md` si registra
el cambio.

## Decisiones y alternativas

| Decisión | Por qué | Alternativas |
|---|---|---|
| Identificadores de código en inglés, interfaz y documentación en español | Es la convención habitual en equipos de desarrollo: el código se lee igual en cualquier empresa, y la documentación está en el idioma del usuario | Todo en español (`Producto`, `Precio`): se mezcla con palabras clave y librerías en inglés y queda raro |
| Especificaciones como **columnas anulables** en `products` | El agente tiene que filtrar por ellas con SQL sencillo (`WHERE ram_gb >= 8`), y solo son tres | Tabla aparte `ProductSpecs` (1 a 1): más normalizada, pero obliga a un `JOIN` en cada consulta. Columna **JSONB**: flexible para specs muy distintas por categoría, y EF Core la puede mapear con `ToJson()`; pero las consultas, los índices y la validación son más complejos que con columnas normales, y para tres campos no compensa |
| Categoría como **enum** guardado como texto | Solo hay tres categorías y no cambian desde la interfaz; el código puede usarlas con seguridad de tipos | **Tabla de categorías** con clave foránea: mejor si el usuario pudiera crear categorías nuevas, que no es el caso |
| `decimal` para el precio | Representación exacta en base 10: `0.1 + 0.2` da `0.3`. Con `double` aparecen errores de redondeo inaceptables con dinero | `double` (incorrecto para dinero); guardar céntimos en un `int` (válido, pero menos legible) |
| Índice por `Category` y no por `Brand` | Casi todas las preguntas del agente filtran por categoría. Con 8–10 productos ningún índice se nota; se añade el de categoría por coherencia con el uso previsto | Índice también por `Brand`: se añadiría si aparecieran consultas frecuentes por marca |
| Commit solo de documentación | El código de `Product` pertenece al paso 3, que aún no está terminado (falta registrar el `DbContext`, la migración y el *seed*). Así cada commit representa un estado coherente | Un solo commit con el código a medias del paso 3: mezclaría dos cambios distintos en el historial |

## Para la entrevista

**Frases que puedo decir:**

> "La aplicación es un asistente para una tienda de tecnología con dos fuentes de
> información: el catálogo, que son datos estructurados en PostgreSQL, y los
> documentos de la tienda, que son texto libre indexado con embeddings en pgvector.
> El agente decide si consulta el catálogo con SQL, busca en los documentos con RAG,
> o combina las dos cosas."

> "Cambié el dominio antes de crear la primera migración, así que fue barato. Si
> hubiera estado en producción, no habría tocado las migraciones existentes: habría
> añadido una nueva que transforme el esquema y los datos."

**Posibles preguntas:**

- *¿Cuándo usas una herramienta SQL y cuándo RAG?*
  SQL cuando la respuesta depende de valores exactos: filtrar, ordenar, contar,
  comparar precios o especificaciones. RAG cuando la respuesta está escrita en un
  texto, como una política de devoluciones. Los embeddings no saben comparar
  números, y SQL no sabe encontrar un párrafo por su significado.

- *¿Por qué `decimal` y no `double` para el precio?*
  Porque `double` es binario y no representa exactamente valores como 0,10. Con
  dinero los errores de redondeo no son aceptables. `decimal` trabaja en base 10.

- *¿Por qué las especificaciones son anulables?*
  Porque no aplican a todos los productos: una PS5 no tiene pantalla y una
  Nintendo Switch sí, así que ni siquiera depende de la categoría. `null`
  significa "no aplica", mientras que `0` sería un dato falso.

- *¿Se pueden borrar migraciones?*
  En desarrollo, si nadie más las ha aplicado, sí. En producción no: la base tiene
  registradas en `__EFMigrationsHistory` las que ya se aplicaron y contiene datos
  reales. Se añade una migración nueva que haga la transformación.

## Pendiente

- **Paso 3:** datos de ejemplo (*seed*) con 8–10 productos realistas repartidos
  entre móviles, ordenadores y consolas.
- **Paso 7:** entidad `Document` con relación **opcional** a `Product`: los
  manuales pertenecen a un producto; la garantía, las devoluciones y los envíos son
  documentos generales de la tienda.
