# SqlDbAnalyze.Abstractions

Contracts and lightweight abstractions for the SQL DB DTU analysis tool.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/patterns.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/domain.md

## Module Purpose

Defines domain models (DtuMetric, DatabaseDtuSummary, ElasticPoolRecommendation), service interfaces (IAzureMetricsService, IDtuAnalysisService, IServerAnalysisService), and custom exceptions. All types are persistence-ignorant and framework-free.

## Key Contents

- `Interfaces/IAzureMetricsService.cs` — contract for fetching DTU metrics and database info from Azure
- `Interfaces/IDtuAnalysisService.cs` — contract for DTU aggregation and elastic pool recommendation logic
- `Interfaces/IServerAnalysisService.cs` — contract for orchestrating full server analysis
- `Models/DtuMetric.cs` — single DTU measurement record
- `Models/DatabaseDtuSummary.cs` — per-database DTU summary
- `Models/ElasticPoolRecommendation.cs` — elastic pool sizing recommendation
- `Models/HourlyDtuAggregate.cs` — hourly DTU aggregation
- `Exceptions/AzureResourceNotFoundException.cs` — thrown when Azure resource lookup fails

## Dependency Constraints

- **Allowed**: No project dependencies. Minimal external packages.
- **Forbidden**: Must NOT reference Implementation, Cli, or any framework-specific packages.
