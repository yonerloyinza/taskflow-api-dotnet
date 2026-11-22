# Arquitectura y Despliegue — taskflow-api-c-sharp

- **Stack:** .NET 9 + ASP.NET Core Minimal APIs + Entity Framework Core + FluentValidation
- **Estrategia de Despliegue:** VPS AWS EC2 — Docker multi-etapa (mcr.microsoft.com/dotnet/sdk:9.0-alpine + mcr.microsoft.com/dotnet/aspnet:9.0-alpine) + Traefik HTTPS
- **Base de Datos Cloud Dual:** PostgreSQL gestionado en la nube, alternable entre InsForge Cloud y Supabase Cloud únicamente mediante variables de entorno (`DATABASE_URL` con pooling puerto 6543 para runtime, `DIRECT_URL` puerto 5432 para migraciones) cargadas desde `/opt/taskflow/.env.csharp` en el VPS. Regla estricta: Prohibido instalar o importar SDKs propietarios; toda persistencia debe pasar por EF Core.
- **Contrato API:** OpenAPI 3.1 compartido (`docs/openapi.yaml`) — rutas, DTOs, filtros (`status`, `priority`, `assignee`), paginación y formato de errores `ErrorResponse` respetados al 100%.
- **Health Check:** `GET /health` verificando conexión activa a PostgreSQL Cloud, escuchando en `0.0.0.0:3000`.
- **Red:** Traefik termina TLS y enruta HTTPS → puerto interno 3000 del contenedor (sin exponer puertos en el host).
- **CI/CD:** GitHub Actions (.github/workflows/deploy.yml) → SSH al VPS → `git pull` + `docker compose up -d --build --no-deps api-csharp` + `docker image prune -f` en `/opt/taskflow`. Build local en VPS, sin Docker Hub / ghcr.io.
- **Prohibido:** Monorepos, SQLite, migraciones manuales sin EF Core, Render, SnapDeploy, registros Docker externos.