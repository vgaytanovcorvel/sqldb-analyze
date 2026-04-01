# SqlDbAnalyze.Cli

CLI entry point for the SQL DB DTU analysis tool.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/logging.md
@../../rules/common/patterns.md
@../../rules/common/security.md
@../../rules/common/command-line.md
@../../rules/csharp/coding-style.md
@../../rules/csharp/services.md
@../../rules/csharp/hosting.md
@../../rules/csharp/command-line.md
@../../rules/csharp/security.md

## Module Purpose

Console application entry point using System.CommandLine. Wires DI via IHost, defines the `analyze` command that accepts a SQL Server name and outputs DTU analysis with elastic pool recommendations.

## Key Contents

- `Program.cs` — RootCommand, global options, CommandLineBuilder with UseHost() for DI
- `Commands/AnalyzeCommand.cs` — `analyze` subcommand: accepts server name, subscription, resource group, hours; outputs DTU summary table and elastic pool recommendation

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Abstractions, SqlDbAnalyze.Implementation, System.CommandLine, Microsoft.Extensions.Hosting
- **Forbidden**: Must NOT reference Web.* assemblies.
