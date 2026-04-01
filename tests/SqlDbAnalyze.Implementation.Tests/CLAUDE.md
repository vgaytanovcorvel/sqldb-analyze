# SqlDbAnalyze.Implementation.Tests

Unit tests for the Implementation module.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/testing.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/testing.md

## Module Purpose

Tests for DtuAnalysisService, ServerAnalysisService, and AzureMetricsService. Unit tests mock Azure SDK dependencies and verify DTU aggregation, summarization, and recommendation logic.

## Key Contents

- `PlaceholderTests.cs` — initial placeholder to verify project compiles

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Implementation (and transitively Abstractions), xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk
- **Forbidden**: Must NOT reference Cli.
