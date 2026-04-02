# SqlDbAnalyze

CLI tool that connects to Azure and analyzes SQL Server database DTU usage over time to recommend elastic pool sizing.

## Architecture

Clean architecture .NET 9 solution with System.CommandLine CLI entry point.

## Rules

Rules are deployed in `/rules/` and referenced by each module's CLAUDE.md via `@` paths. Do not duplicate rule content here.

- `rules/common/` — language-agnostic: coding style, patterns, logging, security, testing, CLI design
- `rules/csharp/` — C#-specific: coding style, domain, services, hosting, CLI, security, testing

## Modules

### Source (`src/`)

| Module | Purpose |
|---|---|
| `SqlDbAnalyze.Abstractions` | Domain models, service interfaces, exceptions |
| `SqlDbAnalyze.Implementation` | Azure metrics fetching, DTU analysis, elastic pool recommendation, correlation-aware pool optimization, CSV I/O, DI registration |
| `SqlDbAnalyze.Cli` | System.CommandLine CLI entry point with `analyze`, `capture`, and `build-pools` commands |

### Tests (`tests/`)

| Module | Tests |
|---|---|
| `SqlDbAnalyze.Abstractions.Tests` | Domain model tests |
| `SqlDbAnalyze.Implementation.Tests` | Service logic unit tests |
| `SqlDbAnalyze.Cli.Tests` | CLI command parsing and handler tests |

## Dependency Flow

```
Cli → Implementation → Abstractions
Cli → Abstractions
```

- **Abstractions**: No project dependencies
- **Implementation**: Abstractions only
- **Cli**: Abstractions + Implementation

## Key Patterns

- **Azure SDK**: `Azure.ResourceManager.Sql` for ARM queries, `Azure.Monitor.Query` for DTU metrics
- **Authentication**: `DefaultAzureCredential` (Azure CLI, managed identity, etc.)
- **DI**: Wired via `IHost` + `CommandLineBuilder.UseHost()`
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
