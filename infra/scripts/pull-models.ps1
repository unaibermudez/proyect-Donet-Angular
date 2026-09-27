# Descarga los modelos que usa el proyecto dentro del contenedor de Ollama.
# Uso:  pwsh ./infra/scripts/pull-models.ps1
# Requiere que `docker compose up -d ollama` esté ejecutándose.

$ErrorActionPreference = 'Stop'

$models = @(
    'nomic-embed-text',  # embeddings, 768 dimensiones, ~275 MB
    'llama3.1:8b'        # chat con soporte de tool calling, ~4.7 GB
)

foreach ($model in $models) {
    Write-Host "==> Descargando $model ..." -ForegroundColor Cyan
    docker compose exec -T ollama ollama pull $model
}

Write-Host "==> Modelos disponibles:" -ForegroundColor Green
docker compose exec -T ollama ollama list
