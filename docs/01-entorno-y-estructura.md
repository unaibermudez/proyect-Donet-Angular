# 01 · Entorno y estructura del repositorio

## Qué hemos hecho

Hemos preparado el "esqueleto" del proyecto antes de escribir una sola línea de código de aplicación:

- Repositorio git inicializado con la rama `main`.
- Estructura de carpetas: `backend/`, `frontend/`, `docs/`, `infra/` y `.github/workflows/`.
- Archivos de configuración del repo: `.gitignore`, `.gitattributes` y `.editorconfig`.
- Un `docker-compose.yml` que levanta **PostgreSQL con pgvector** y **Ollama**, los dos servicios de infraestructura que el proyecto necesita.
- `.env.example` como plantilla de variables de entorno (el `.env` real está ignorado por git, así que nunca se sube nada sensible).
- Un script `infra/scripts/pull-models.ps1` para descargar los modelos de IA.
- `README.md` con la descripción del proyecto y un diagrama de arquitectura en Mermaid, y `AI_REVIEW.md` para registrar las correcciones al código generado.

La decisión de fondo: **en desarrollo solo la infraestructura va en Docker**. El backend se ejecuta con `dotnet run` y el frontend con `ng serve`, para tener recarga en caliente y depuración cómoda. En el paso 12 añadiremos también los contenedores de `api` y `web`, que es lo que se usaría en CI y en producción.

## Conceptos nuevos

### `docker-compose.yml` sin clave `version`

En las versiones modernas de Docker Compose la clave `version:` está obsoleta y produce un aviso. El archivo empieza directamente por `services:`.

*Equivalente:* es el mismo `docker-compose.yml` que usarías con Spring Boot para levantar Postgres. Ningún cambio conceptual.

### pgvector

Es una **extensión de PostgreSQL** que añade un tipo de dato `vector` y operadores de distancia (`<=>` para coseno, `<->` para euclídea), más índices especializados (HNSW, IVFFlat) para buscar los vectores más parecidos sin recorrer toda la tabla.

Usamos la imagen `pgvector/pgvector:pg17`, que es Postgres con la extensión ya compilada dentro. Aun así hay que ejecutar `CREATE EXTENSION vector;` en cada base de datos: eso lo hace `infra/db/init/01-extensions.sql`.

*Equivalente:* en el mundo Spring Boot sería lo mismo que usar pgvector con Hibernate y el tipo `vector` mapeado a mano, o Spring AI con su `PgVectorStore`. El concepto es idéntico: la base de datos relacional que ya tienes hace también de base de datos vectorial, y te ahorras montar Pinecone, Qdrant o Weaviate.

### Ollama

Un servidor que ejecuta modelos de lenguaje en tu propio ordenador y expone una API HTTP en el puerto `11434`. Lo usamos para dos cosas distintas:

| Modelo | Para qué | Tamaño aprox. |
|---|---|---|
| `nomic-embed-text` | Generar **embeddings** (vectores de 768 dimensiones) | ~275 MB |
| `llama3.1:8b` | **Chat** y *tool calling* del agente | ~4,7 GB |

No todos los modelos sirven para las dos cosas: un modelo de embeddings no conversa, y no todos los modelos de chat soportan *tool calling*. `llama3.1` sí, y por eso lo elegimos pensando en el paso 11.

*Equivalente:* no hay uno directo en Spring Boot. Lo más parecido es tener un LocalStack o un WireMock que te evita depender de un servicio de pago mientras desarrollas. La ventaja aquí es real: coste cero y sin claves de API.

### El doble guion bajo en las variables de entorno

En .NET la configuración es jerárquica (`AI:Ollama:ChatModel`). Como los dos puntos no son válidos en variables de entorno en todos los sistemas, .NET traduce **doble guion bajo** a separador de nivel:

```
AI__Ollama__ChatModel=llama3.1:8b   →   AI:Ollama:ChatModel
```

*Equivalente:* es exactamente el mecanismo de *relaxed binding* de Spring Boot, donde `SPRING_DATASOURCE_URL` mapea a `spring.datasource.url`. Misma idea, sintaxis distinta.

### `.editorconfig`

Archivo estándar que los IDE respetan para unificar indentación, codificación y final de línea. En .NET va más allá: el compilador de C# lee de aquí las reglas de estilo (`csharp_style_*`, `dotnet_style_*`) y emite avisos cuando el código no las cumple.

*Equivalente:* la combinación de `.editorconfig` + Prettier + ESLint en un proyecto React, pero integrado en el propio compilador en vez de ser una herramienta aparte.

### `.gitattributes` con `eol=lf`

Estás en Windows, así que git podría meter finales de línea `CRLF` en archivos que luego se ejecutan dentro de contenedores Linux (un script `.sh` con `CRLF` falla con un error confuso). Forzamos `LF` en todo menos en los archivos que solo tienen sentido en Windows (`.ps1`, `.sln`).

## Archivos importantes

| Archivo | Qué hace |
|---|---|
| `docker-compose.yml` | Define los servicios `db` (Postgres+pgvector, puerto 5433) y `ollama` (puerto 11434), con volúmenes persistentes y un *healthcheck* en la base de datos |
| `.env.example` | Plantilla de variables de entorno: credenciales de la base, puertos y configuración del proveedor de IA. Se copia a `.env`, que está en `.gitignore` |
| `infra/db/init/01-extensions.sql` | Ejecuta `CREATE EXTENSION IF NOT EXISTS vector` la primera vez que se crea el volumen de la base de datos |
| `infra/scripts/pull-models.ps1` | Descarga `nomic-embed-text` y `llama3.1:8b` dentro del contenedor de Ollama |
| `.gitignore` | Ignora `bin/`, `obj/`, `node_modules/`, `.angular/`, `.env`, la carpeta de ficheros subidos y los archivos de IDE |
| `.editorconfig` | Indentación de 4 espacios en C#, 2 en el resto; reglas de estilo de C# que el compilador verifica |
| `.gitattributes` | Normaliza finales de línea a `LF` para que los scripts funcionen en contenedores Linux |
| `README.md` | Descripción, tabla de stack, diagrama Mermaid de arquitectura, cómo arrancar e índice de `/docs` |
| `AI_REVIEW.md` | Registro de las correcciones aplicadas al código generado por IA |

## Cómo probarlo

Con Docker Desktop ya arrancado:

```bash
cd C:/dev/PERSONAL/proyecto-C#-Angular

# 1. Crear el .env a partir de la plantilla
cp .env.example .env

# 2. Levantar la infraestructura
docker compose up -d

# 3. Ver que los dos contenedores están arriba y la base en estado healthy
docker compose ps
```

Comprobar que pgvector está disponible:

```bash
docker compose exec db psql -U docassist -d docassist -c "SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';"
```

Debe devolver una fila con `vector` y su versión.

Comprobar que Ollama responde:

```bash
curl http://localhost:11434/api/version
```

Descargar los modelos (tarda unos minutos la primera vez):

```bash
pwsh ./infra/scripts/pull-models.ps1
```

Y verificar que el .NET SDK está instalado (necesario para el paso 2):

```bash
dotnet --list-sdks
```

Para parar la infraestructura:

```bash
docker compose stop        # parar
docker compose down        # parar y borrar contenedores (los volúmenes se mantienen)
docker compose down -v     # borrar también los datos y los modelos descargados
```

## Decisiones y alternativas

| Decisión | Por qué | Alternativas descartadas |
|---|---|---|
| Postgres con pgvector como base vectorial | Una sola base de datos para los datos relacionales y los vectores: menos piezas que mantener y transacciones que abarcan ambas cosas. Y es lo que pide la oferta | Qdrant, Pinecone o Weaviate: más potentes a gran escala, pero añaden un servicio extra y un modelo de datos separado |
| Ollama en local por defecto | Coste cero y sin claves de API, y permite demostrar la abstracción de proveedor. Los modelos pequeños bastan para una demo | Empezar directamente con Azure OpenAI o Anthropic: mejor calidad de respuesta, pero gasto y dependencia de red durante el desarrollo |
| Solo infraestructura en Docker durante el desarrollo | Recarga en caliente y depuración directa desde el IDE. Reconstruir la imagen del backend en cada cambio sería lentísimo | Todo en Compose desde el principio: más fiel a producción, mucho más incómodo para iterar |
| Puerto `5433` para Postgres | Ya trabajas con PostgreSQL a diario; es muy probable que el 5432 esté ocupado en tu máquina | Usar 5432 y arriesgarse a un conflicto de puertos confuso |
| Monorepo con `backend/` y `frontend/` | Un solo repo, un solo pipeline de CI, y se ve de un golpe cómo encajan las dos mitades | Dos repositorios separados: más realista en una empresa grande, peor para enseñar el proyecto completo |
| `llama3.1:8b` como modelo de chat | Soporta *tool calling*, imprescindible para el paso 11 | `phi3` o `gemma2`: más ligeros, pero con soporte de herramientas peor o inexistente |

## Para la entrevista

**Frases que puedo decir:**

> "Monté la infraestructura con Docker Compose: Postgres con pgvector y Ollama. Elegí pgvector en lugar de una base vectorial dedicada porque el volumen de datos no lo justifica y así mantengo los productos, los documentos y los embeddings en la misma base, con transacciones consistentes."

> "Uso Ollama en local para desarrollar sin coste ni claves de API, pero el proveedor es intercambiable por configuración gracias a las abstracciones de `Microsoft.Extensions.AI`. La configuración de IA vive en variables de entorno y `.env` está en `.gitignore`: no hay ninguna credencial en el repositorio."

> "En desarrollo solo containerizo la infraestructura, no la aplicación, para no perder la recarga en caliente. Los Dockerfile del backend y del frontend los añadí al final, cuando ya hacían falta para CI."

**Posibles preguntas:**

- *¿Por qué pgvector y no una base de datos vectorial dedicada?*
  Porque para este volumen de datos no compensa añadir otro servicio. Con pgvector tengo los datos relacionales y los vectores en la misma base, puedo filtrar por metadatos con SQL normal antes de la búsqueda vectorial, y todo entra en la misma transacción. Con millones de vectores y mucha carga de consulta, replantearía la decisión y miraría Qdrant.

- *¿Qué diferencia hay entre un modelo de embeddings y un modelo de chat?*
  El de embeddings convierte texto en un vector numérico que captura su significado, para poder comparar textos por similitud; no genera lenguaje. El de chat genera texto. En RAG hacen falta los dos: el de embeddings para encontrar los fragmentos relevantes y el de chat para redactar la respuesta.

- *¿Cómo cambiarías de Ollama a un proveedor en la nube?*
  Cambiando `AI__Provider` y las variables del proveedor nuevo. El registro de dependencias devuelve una implementación distinta de `IChatClient` y de `IEmbeddingGenerator`, y el resto del código no se toca porque solo conoce las interfaces.

- *¿Dónde guardas las claves de API?*
  En variables de entorno en local (`.env`, ignorado por git) y en los secretos del proveedor de despliegue o en Azure Key Vault en producción. Nunca en el código ni en `appsettings.json` versionado.
