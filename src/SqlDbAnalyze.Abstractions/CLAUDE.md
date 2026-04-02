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
- `Interfaces/IStatisticsService.cs` — contract for statistical computations (mean, percentile, correlation)
- `Interfaces/ITimeSeriesCsvService.cs` — contract for CSV time series I/O
- `Interfaces/IPoolabilityService.cs` — contract for pairwise poolability analysis
- `Interfaces/IPlacementScorer.cs` — contract for scoring database placement into pools
- `Interfaces/IPoolBuilder.cs` — contract for greedy pool construction
- `Interfaces/ILocalSearchOptimizer.cs` — contract for local search pool improvement
- `Interfaces/ICaptureService.cs` — contract for capturing DTU metrics from Azure
- `Models/DtuMetric.cs` — single DTU measurement record
- `Models/DatabaseDtuSummary.cs` — per-database DTU summary
- `Models/ElasticPoolRecommendation.cs` — elastic pool sizing recommendation
- `Models/HourlyDtuAggregate.cs` — hourly DTU aggregation
- `Models/DtuTimeSeries.cs` — aligned multi-database DTU time series
- `Models/DatabaseProfile.cs` — pre-computed statistical profile for a database
- `Models/PoolabilityMetrics.cs` — pairwise poolability assessment
- `Models/PoolOptimizerOptions.cs` — configuration for the pool optimizer
- `Models/PlacementScore.cs` — scoring result for a candidate pool placement
- `Models/PoolAssignment.cs` — pool with member databases and sizing
- `Models/PoolOptimizationResult.cs` — full optimizer output
- `Exceptions/AzureResourceNotFoundException.cs` — thrown when Azure resource lookup fails

## Dependency Constraints

- **Allowed**: No project dependencies. Minimal external packages.
- **Forbidden**: Must NOT reference Implementation, Cli, or any framework-specific packages.
