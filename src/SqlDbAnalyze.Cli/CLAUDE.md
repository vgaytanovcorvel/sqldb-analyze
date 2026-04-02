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

Console application entry point using System.CommandLine. Wires DI via IHost, defines `analyze`, `capture`, and `build-pools` commands.

## Key Contents

- `Program.cs` — RootCommand, global options, CommandLineBuilder with UseHost() for DI
- `RootCommandFactory.cs` — creates RootCommand and registers all subcommands
- `Commands/AnalyzeCommand.cs` — `analyze` subcommand: DTU summary table and elastic pool recommendation
- `Commands/CaptureCommand.cs` — `capture` subcommand: exports DTU time series to CSV
- `Commands/BuildPoolsCommand.cs` — `build-pools` subcommand: correlation-aware pool optimization from CSV
- `Commands/TimeWindowParser.cs` — shared utility for parsing time window options

## Dependency Constraints

- **Allowed**: SqlDbAnalyze.Abstractions, SqlDbAnalyze.Implementation, System.CommandLine, Microsoft.Extensions.Hosting
- **Forbidden**: Must NOT reference Web.* assemblies.
