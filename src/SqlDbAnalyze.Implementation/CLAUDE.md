# SqlDbAnalyze.Implementation

Business logic and Azure integration for DTU analysis and elastic pool recommendations.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/logging.md
@../../rules/common/patterns.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/services.md

## Module Purpose

Implements Azure Monitor metrics fetching, DTU aggregation/summarization, and elastic pool recommendation logic. Contains all service implementations and DI registration.

## Key Contents

- `Services/AzureMetricsService.cs` — fetches database lists, DTU metrics, and DTU limits from Azure via ARM and Monitor SDKs
- `Services/DtuAnalysisService.cs` — aggregates DTU metrics by hour, summarizes per-database, and recommends elastic pool sizing
- `Services/ServerAnalysisService.cs` — orchestrates full server analysis by combining metrics fetching and analysis
- `Extensions/SqlDbAnalyzeServiceCollectionExtensions.cs` — DI registration for all services and Azure SDK clients

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Abstractions, Azure SDK packages, Microsoft.Extensions.DependencyInjection.Abstractions
- **Forbidden**: Must NOT reference Cli or any presentation layer.
