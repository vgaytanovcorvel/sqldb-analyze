# SqlDbAnalyze.Cli.Tests

Unit tests for the CLI module.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/testing.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/testing.md

## Module Purpose

Tests for CLI command parsing, argument validation, and handler behavior. Tests invoke commands via System.CommandLine's test infrastructure with mocked services.

## Key Contents

- `PlaceholderTests.cs` — initial placeholder to verify project compiles

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Cli (and transitively Implementation, Abstractions), xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk
- **Forbidden**: No additional restrictions.
