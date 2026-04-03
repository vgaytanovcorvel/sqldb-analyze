# SqlDbAnalyze.Repository

Data access layer using Entity Framework Core with SQLite.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/database.md
@../../rules/common/patterns.md
@../../rules/common/security.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/persistence.md
@../../rules/csharp/security.md

## Module Purpose

Implements data persistence using EF Core with SQLite. Contains DbContext, ORM entity classes, repository implementations, and entity-to-domain model mapping. Repositories operate on domain models externally while using ORM entities internally.

## Key Contents

- `Contexts/AppDbContext.cs` — EF Core DbContext configured for SQLite
- `RepositoryBase.cs` — abstract base class accepting IDbContextFactory for thread-safe repository access
- `Entities/` — ORM entity classes (with navigation properties, EF attributes)
- `Repositories/` — repository implementations mapping between domain models and entities
- `Extensions/PersistenceServiceCollectionExtensions.cs` — DI registration for DbContextFactory and repositories

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Abstractions, Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite
- **Forbidden**: Must NOT reference Implementation, Web.*, or Cli.
