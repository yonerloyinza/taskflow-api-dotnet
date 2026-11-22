# Roadmap — taskflow-api-c-sharp

- [ ] **Paso 0 — Aclaración de contexto:** Verificar variables de entorno y puertos locales.
- [ ] **Paso 1 — Estructura Base e Infraestructura:** Scaffolding ASP.NET Core Minimal APIs, .NET 9 estricto, seguridad (CORS, Rate Limit, Security Headers), FluentValidation, Global Error Handling.
- [ ] **Paso 2 — Persistencia y Migraciones Duales:** EF Core + modelo Task, dual connection (`DATABASE_URL` pooler 6543, `DIRECT_URL` 5432), migraciones EF Core aplicadas a InsForge Cloud.
- [ ] **Paso 3 — Implementación del Contrato (`docs/openapi.yaml`):** Endpoints CRUD Tasks, filtros (`status`, `priority`, `assignee`), paginación, manejo errores `ErrorResponse`, `GET /health` con verificación DB en `0.0.0.0:3000`.
- [ ] **Paso 4 — Testing:** Suite unitarias (Service layer) + integración (Endpoints) con xUnit + WebApplicationFactory, 25 tests passing.
- [ ] **Paso 5a — Documentación y Docker:** AGENTS.md actualizado, docs/01-architecture.md, Dockerfile multi-etapa (.NET 9 Alpine) para VPS AWS + Traefik.
- [ ] **Paso 5b — CI/CD VPS:** GitHub Actions deploy.yml → SSH → `git pull` + `docker compose up -d --build --no-deps api-csharp` + `docker image prune -f` en `/opt/taskflow`. Variables en `/opt/taskflow/.env.csharp`. Sin Render, sin registros Docker externos.