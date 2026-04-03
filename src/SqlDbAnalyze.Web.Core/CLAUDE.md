# SqlDbAnalyze.Web.Core

Core web functionality — controllers and web-specific services.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/logging.md
@../../rules/common/patterns.md
@../../rules/common/security.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/services.md
@../../rules/csharp/presentation.md
@../../rules/csharp/security.md

## Module Purpose

Contains API controllers and web-specific services for the SqlDbAnalyze application. Controllers are thin — they delegate business logic to services from Implementation.

## Key Contents

- `Controllers/HealthController.cs` — health check endpoint

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Abstractions, SqlDbAnalyze.Implementation, Microsoft.AspNetCore.OpenApi
- **Forbidden**: Must NOT reference Repository, Cli, or Web.Api directly.
