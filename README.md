# sqldb-analyze

CLI tool that connects to Azure and analyzes SQL Server database DTU usage over time to recommend elastic pool sizing.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An Azure subscription with access to the target SQL Server
- Azure CLI authenticated (`az login`) or another credential source supported by [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)

## Build

```bash
dotnet build
```

## Run

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze <server-name> \
  --subscription <subscription-id> \
  --resource-group <resource-group-name> \
  [--hours <hours>] \
  [--window-start <HH:mm>] \
  [--window-end <HH:mm>] \
  [--window-timezone <timezone>]
```

### Arguments

| Argument | Description |
|---|---|
| `server-name` | Name of the Azure SQL Server to analyze (required) |

### Options

| Option | Short | Default | Description |
|---|---|---|---|
| `--subscription` | `-s` | *(required)* | Azure subscription ID |
| `--resource-group` | `-g` | *(required)* | Azure resource group name |
| `--hours` | | `24` | Number of hours of metrics to analyze |
| `--window-start` | | *(none)* | Start of daily analysis window in HH:mm format |
| `--window-end` | | *(none)* | End of daily analysis window in HH:mm format |
| `--window-timezone` | | `Eastern Standard Time` | Time zone for the analysis window |
| `--verbose` | `-v` | `false` | Increase output detail |

## Examples

### Basic analysis (last 24 hours, all hours of day)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group"
```

### Analyze the last 48 hours

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 48
```

### Business hours only (East Coast morning to Pacific end of day)

9 AM Eastern to 5 PM Pacific is 9 AM - 8 PM Eastern:

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 168 \
  --window-start 09:00 \
  --window-end 20:00
```

The same window expressed in Pacific time:

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 168 \
  --window-start 06:00 \
  --window-end 17:00 \
  --window-timezone "Pacific Standard Time"
```

### Standard US business hours (9-5 Eastern)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 168 \
  --window-start 09:00 \
  --window-end 17:00
```

### Overnight window (off-hours analysis)

The window wraps around midnight when start > end:

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --window-start 22:00 \
  --window-end 06:00
```

## Sample Output

```
Analyzing DTU usage for server 'my-sql-server' over the last 168 hours...
  Time window: 09:00 - 20:00 (Eastern Standard Time)

Database DTU Summary:
----------------------------------------------------------------------
Database                         Avg DTU%   Peak DTU%  DTU Limit
----------------------------------------------------------------------
app-primary                          35.2%      87.4%        100
app-reporting                        12.8%      45.1%         50
app-staging                           4.1%      18.3%         20

Elastic Pool Recommendation:
  Tier:           Standard
  Pool DTUs:      100
  Est. Total DTU: 42.6
```

## Time Window Notes

- Both `--window-start` and `--window-end` must be provided together.
- Times are in 24-hour HH:mm format.
- The window is inclusive of start and exclusive of end: `[start, end)`.
- When `--window-start` is later than `--window-end`, the window wraps around midnight (e.g., 22:00-06:00 covers nighttime hours).
- `--window-timezone` defaults to `Eastern Standard Time`. Common values:
  - `Eastern Standard Time`
  - `Central Standard Time`
  - `Mountain Standard Time`
  - `Pacific Standard Time`
  - `UTC`
- Daylight saving is handled automatically by the .NET runtime.

## Authentication

The tool uses [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential), which tries these credential sources in order:

1. Environment variables
2. Workload identity
3. Managed identity
4. Azure CLI (`az login`)
5. Azure PowerShell (`Connect-AzAccount`)
6. Azure Developer CLI (`azd auth login`)
7. Interactive browser

For local development, run `az login` before using the tool.

## Tests

```bash
dotnet test
```

## Architecture

Clean architecture .NET 9 solution:

```
src/
  SqlDbAnalyze.Abstractions   Domain models, interfaces, exceptions
  SqlDbAnalyze.Implementation Azure metrics, DTU analysis, recommendations
  SqlDbAnalyze.Cli            System.CommandLine entry point

tests/
  SqlDbAnalyze.Abstractions.Tests
  SqlDbAnalyze.Implementation.Tests
  SqlDbAnalyze.Cli.Tests
```

Dependency flow: `Cli -> Implementation -> Abstractions`
