# SqlDbAnalyze.Repository.Tests

Unit tests for the Repository module.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/testing.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/testing.md

## Module Purpose

Tests for repository implementations and data access logic. Uses in-memory SQLite for integration tests and verifies entity-to-domain mapping, query correctness, and DbContext configuration.

## Key Contents

- `PlaceholderTests.cs` — initial placeholder to verify project compiles

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Repository (and transitively Abstractions), xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk
- **Forbidden**: Must NOT reference Implementation, Cli, or Web.*.
