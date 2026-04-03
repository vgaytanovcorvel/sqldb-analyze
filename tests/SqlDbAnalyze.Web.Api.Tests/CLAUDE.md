# SqlDbAnalyze.Web.Api.Tests

Unit and integration tests for the Web.Api host module.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/testing.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/testing.md

## Module Purpose

Tests for the Web.Api host including middleware pipeline, DI wiring, and integration tests via WebApplicationFactory. Verifies the API host starts correctly and endpoints are reachable.

## Key Contents

- `PlaceholderTests.cs` — initial placeholder to verify project compiles

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Web.Api (and transitively all source assemblies), xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk
- **Forbidden**: No additional restrictions.
