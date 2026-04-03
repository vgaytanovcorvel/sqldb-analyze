# SqlDbAnalyze

CLI tool and web application that connects to Azure and analyzes SQL Server database DTU usage over time to recommend elastic pool sizing.

## Architecture

Clean architecture .NET 9 solution with System.CommandLine CLI entry point and ASP.NET Core Web API backend with React frontend.

## Rules

Rules are deployed in `/rules/` and referenced by each module's CLAUDE.md via `@` paths. Do not duplicate rule content here.

- `rules/common/` — language-agnostic: coding style, patterns, logging, security, testing, CLI design
- `rules/csharp/` — C#-specific: coding style, domain, services, persistence, presentation, hosting, CLI, security, testing
- `rules/typescript/` — TypeScript-specific: coding style, CSS, frontend architecture, React, patterns, security, testing

## Modules

### Source (`src/`)

| Module | Purpose |
|---|---|
| `SqlDbAnalyze.Abstractions` | Domain models, service interfaces, exceptions |
| `SqlDbAnalyze.Implementation` | Azure metrics fetching, DTU analysis, elastic pool recommendation, correlation-aware pool optimization, CSV I/O, DI registration |
| `SqlDbAnalyze.Repository` | EF Core data access layer with SQLite persistence |
| `SqlDbAnalyze.Web.Core` | API controllers and web-specific services |
| `SqlDbAnalyze.Web.Api` | ASP.NET Core Web API host (Swagger, middleware pipeline, DI wiring) |
| `SqlDbAnalyze.Cli` | System.CommandLine CLI entry point with `analyze`, `capture`, and `build-pools` commands |
| `sqldbanalyze.client` | React + TypeScript SPA frontend (Vite, React Query, Zustand) |

### Tests (`tests/`)

| Module | Tests |
|---|---|
| `SqlDbAnalyze.Abstractions.Tests` | Domain model tests |
| `SqlDbAnalyze.Implementation.Tests` | Service logic unit tests |
| `SqlDbAnalyze.Repository.Tests` | Repository and data access tests |
| `SqlDbAnalyze.Web.Core.Tests` | Controller unit tests |
| `SqlDbAnalyze.Web.Api.Tests` | API host integration tests |
| `SqlDbAnalyze.Cli.Tests` | CLI command parsing and handler tests |

## Dependency Flow

```
Web.Api → Web.Core → Implementation → Abstractions
Web.Api → Implementation
Web.Api → Repository → Abstractions
Cli → Implementation → Abstractions
Cli → Abstractions
sqldbanalyze.client → (HTTP) → Web.Api
```

- **Abstractions**: No project dependencies
- **Implementation**: Abstractions only
- **Repository**: Abstractions only
- **Web.Core**: Abstractions + Implementation
- **Web.Api**: Web.Core + Implementation + Repository
- **Cli**: Abstractions + Implementation
- **sqldbanalyze.client**: No .NET dependencies (HTTP only)

## Key Patterns

- **Azure SDK**: `Azure.ResourceManager.Sql` for ARM queries, `Azure.Monitor.Query` for DTU metrics
- **Authentication**: `DefaultAzureCredential` (Azure CLI, managed identity, etc.)
- **DI**: Wired via `IHost` + `CommandLineBuilder.UseHost()` (CLI) and `WebApplication.CreateBuilder` (Web API)
- **Persistence**: EF Core with SQLite via `IDbContextFactory` for thread-safe repository access
- **Frontend**: React + Vite with React Query (server state), Zustand (client state), CSS Modules
- **Central Package Management**: All versions in `Directory.Packages.props`

## Usage

### Analyze (simple DTU summary)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze <server-name> \
  --subscription <sub-id> \
  --resource-group <rg-name> \
  --hours 24
```

### Capture (export time series CSV)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- capture <server-name> \
  --subscription <sub-id> \
  --resource-group <rg-name> \
  --hours 168 \
  --output metrics.csv
```

### Build Pools (correlation-aware pool optimization)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools metrics.csv \
  --target-percentile 0.99 \
  --safety-factor 1.10 \
  --max-dbs-per-pool 50
```

See `docs/correlation-aware-pool-optimization.md` for the full algorithm description.
