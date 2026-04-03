# SqlDbAnalyze.Web.Core.Tests

Unit tests for the Web.Core module.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/testing.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/testing.md

## Module Purpose

Tests for API controllers and web-specific services. Tests verify controller actions return correct responses, delegate to services properly, and handle error cases.

## Key Contents

- `PlaceholderTests.cs` — initial placeholder to verify project compiles

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Web.Core (and transitively Implementation, Abstractions), xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk
- **Forbidden**: Must NOT reference Repository, Cli, or Web.Api.
