# SqlDbAnalyze.Web.Api

ASP.NET Core Web API host for the SqlDbAnalyze application.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/logging.md
@../../rules/common/security.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/persistence.md
@../../rules/csharp/presentation.md
@../../rules/csharp/hosting.md
@../../rules/csharp/security.md

## Module Purpose

Application entry point for the Web API. Configures the middleware pipeline, DI container, Swagger/OpenAPI documentation, CORS, and SQLite persistence. Serves as the backend for the React frontend.

## Key Contents

- `Program.cs` — middleware pipeline, DI wiring, Swagger configuration, CORS setup

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Web.Core, SqlDbAnalyze.Implementation, SqlDbAnalyze.Repository, Swashbuckle.AspNetCore
- **Forbidden**: Must NOT reference Cli. Must NOT reference Web.Server (if it existed).
