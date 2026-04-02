# sqldb-analyze

CLI tool that connects to Azure and analyzes SQL Server database DTU usage over time to recommend elastic pool sizing. Includes a correlation-aware pool optimizer that groups databases based on workload complementarity.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An Azure subscription with access to the target SQL Server
- Azure CLI authenticated (`az login`) or another credential source supported by [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)

## Build

```bash
dotnet build
```

## Commands

The tool provides three commands for a complete analysis workflow:

1. **`analyze`** — Quick DTU summary and simple pool recommendation
2. **`capture`** — Export historical DTU time series to CSV
3. **`build-pools`** — Correlation-aware pool optimization from CSV data

The recommended workflow for production sizing is: `capture` -> `build-pools`.

---

### `analyze` — Quick DTU Summary

Connects to Azure and produces a per-database DTU summary with a simple elastic pool recommendation based on average DTU.

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze <server-name> \
  --subscription <subscription-id> \
  --resource-group <resource-group-name> \
  [--hours <hours>] \
  [--window-start <HH:mm>] \
  [--window-end <HH:mm>] \
  [--window-timezone <timezone>]
```

#### Arguments

| Argument | Description |
|---|---|
| `server-name` | Name of the Azure SQL Server to analyze (required) |

#### Options

| Option | Short | Default | Description |
|---|---|---|---|
| `--subscription` | `-s` | *(required)* | Azure subscription ID |
| `--resource-group` | `-g` | *(required)* | Azure resource group name |
| `--hours` | | `24` | Number of hours of metrics to analyze |
| `--window-start` | | *(none)* | Start of daily analysis window in HH:mm format |
| `--window-end` | | *(none)* | End of daily analysis window in HH:mm format |
| `--window-timezone` | | `Eastern Standard Time` | Time zone for the analysis window |
| `--verbose` | `-v` | `false` | Increase output detail |

---

### `capture` — Export DTU Time Series to CSV

Fetches DTU percentage metrics from Azure Monitor at 5-minute granularity, aligns timestamps across all databases, and writes a CSV file. This CSV is the input for `build-pools`.

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- capture <server-name> \
  --subscription <subscription-id> \
  --resource-group <resource-group-name> \
  [--hours <hours>] \
  [--output <file-path>] \
  [--window-start <HH:mm>] \
  [--window-end <HH:mm>] \
  [--window-timezone <timezone>]
```

#### Arguments

| Argument | Description |
|---|---|
| `server-name` | Name of the Azure SQL Server to capture metrics from (required) |

#### Options

| Option | Short | Default | Description |
|---|---|---|---|
| `--subscription` | `-s` | *(required)* | Azure subscription ID |
| `--resource-group` | `-g` | *(required)* | Azure resource group name |
| `--hours` | | `24` | Number of hours of metrics to capture |
| `--output` | `-o` | `dtu-metrics.csv` | Output CSV file path |
| `--window-start` | | *(none)* | Start of daily analysis window in HH:mm format |
| `--window-end` | | *(none)* | End of daily analysis window in HH:mm format |
| `--window-timezone` | | `Eastern Standard Time` | Time zone for the analysis window |

#### CSV Format

The output CSV has one `Timestamp` column (ISO 8601) followed by one column per database containing DTU percentage values:

```csv
Timestamp,app-primary,app-reporting,app-staging
2026-03-25T00:00:00.0000000+00:00,12.50,3.20,1.10
2026-03-25T00:05:00.0000000+00:00,15.30,4.10,0.80
...
```

---

### `build-pools` — Correlation-Aware Pool Optimization

Reads one or more CSV files (produced by `capture`), analyzes workload correlations, and recommends optimal elastic pool assignments. This treats pool sizing as a capacity packing problem under uncertainty — databases with non-overlapping peaks share pools efficiently.

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools <csv-files...> \
  [--target-percentile <p>] \
  [--safety-factor <f>] \
  [--max-overload <fraction>] \
  [--max-dbs-per-pool <n>] \
  [--peak-threshold <p>] \
  [--max-pool-capacity <dtu>] \
  [--isolate <db1> <db2> ...] \
  [--max-search-passes <n>]
```

#### Arguments

| Argument | Description |
|---|---|
| `csv-files` | One or more CSV files with DTU time series data (required) |

#### Options

| Option | Default | Description |
|---|---|---|
| `--target-percentile` | `0.99` | Percentile of combined load used for pool sizing |
| `--safety-factor` | `1.10` | Multiplier on the target percentile (1.10 = 10% buffer) |
| `--max-overload` | `0.001` | Maximum acceptable fraction of intervals above capacity (0.1%) |
| `--max-dbs-per-pool` | `50` | Maximum number of databases per elastic pool |
| `--peak-threshold` | `0.90` | Percentile threshold for defining "peak" intervals |
| `--max-pool-capacity` | *(none)* | Optional hard cap on pool DTU capacity |
| `--isolate` | *(none)* | Database names that must have their own dedicated pool |
| `--max-search-passes` | `10` | Maximum local search improvement iterations |

#### How It Works

1. **Profile** each database: compute mean, P95, P99, and peak DTU from the time series.
2. **Measure pairwise poolability**: for each pair of databases, compute Pearson correlation, peak-period correlation, and peak overlap fraction. Combine into a "bad-together" score.
3. **Greedy construction**: sort databases by P99 descending, place each into the pool where it increases required capacity the least (subject to overload and size constraints).
4. **Local search improvement**: iteratively try moving databases between pools; accept moves that reduce total required capacity.
5. **Output**: pool assignments with recommended capacity, P95/P99/peak loads, diversification ratio, and overload fraction.

See [`docs/correlation-aware-pool-optimization.md`](docs/correlation-aware-pool-optimization.md) for the full algorithm description.

#### Key Metrics

- **Diversification Ratio** = sum(individual P99) / pooled P99. Higher means better pooling benefit. A ratio of 1.0 means workloads move together (no pooling benefit); 2.0 means the pool needs half the capacity that standalone databases would require.
- **Overload Fraction** = percentage of time intervals where combined load exceeds pool capacity. Should be near zero.

## Examples

### Quick analysis (last 24 hours)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group"
```

### Analyze with time window (business hours only)

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- analyze my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 168 \
  --window-start 09:00 \
  --window-end 17:00
```

### Capture a week of metrics to CSV

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- capture my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 168 \
  -o weekly-metrics.csv
```

### Capture business hours only

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- capture my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 168 \
  --window-start 09:00 \
  --window-end 20:00 \
  -o business-hours.csv
```

### Build pools from captured CSV

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools weekly-metrics.csv
```

### Build pools with custom settings

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools weekly-metrics.csv \
  --target-percentile 0.95 \
  --safety-factor 1.20 \
  --max-dbs-per-pool 20 \
  --peak-threshold 0.85
```

### Isolate critical databases

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools weekly-metrics.csv \
  --isolate payment-db audit-db
```

### Merge metrics from multiple servers

```bash
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools server1.csv server2.csv server3.csv
```

### Full workflow: capture + optimize + constrained capacity

```bash
# Step 1: Capture 30 days of business-hours metrics
dotnet run --project src/SqlDbAnalyze.Cli -- capture my-sql-server \
  -s "00000000-0000-0000-0000-000000000000" \
  -g "my-resource-group" \
  --hours 720 \
  --window-start 06:00 \
  --window-end 22:00 \
  -o month-metrics.csv

# Step 2: Build pools with hard cap and isolated critical DB
dotnet run --project src/SqlDbAnalyze.Cli -- build-pools month-metrics.csv \
  --max-pool-capacity 1600 \
  --isolate payment-db \
  --safety-factor 1.15
```

## Sample Output

### `analyze` output

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

### `capture` output

```
Capturing DTU metrics for server 'my-sql-server' over the last 168 hours...
Captured 12 databases, 2016 data points each.
Output written to: weekly-metrics.csv
```

### `build-pools` output

```
Loaded 12 databases with 2016 time points.

Recommended 3 pool(s), total capacity: 485.0 DTU

----------------------------------------------------------------------------------------------------
Pool     DBs   Capacity        P95        P99       Peak  Divers.   Overload Databases
----------------------------------------------------------------------------------------------------
0          5      220.0      185.3      198.7      245.1      1.82    0.0005% app-web, app-api, app-jobs, app-search, app-cache
1          4      165.0      138.2      152.4      189.3      1.65    0.0000% app-reporting, app-analytics, app-export, app-etl
2          2      100.0       82.1       91.5      112.8      1.41    0.0010% app-staging, app-dev

Isolated databases: payment-db
```

## Time Window Notes

Time window options are available on both `analyze` and `capture` commands.

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
  SqlDbAnalyze.Implementation Azure metrics, DTU analysis, pool optimization, CSV I/O
  SqlDbAnalyze.Cli            System.CommandLine entry point (analyze, capture, build-pools)

tests/
  SqlDbAnalyze.Abstractions.Tests
  SqlDbAnalyze.Implementation.Tests
  SqlDbAnalyze.Cli.Tests

docs/
  correlation-aware-pool-optimization.md   Algorithm description
```

Dependency flow: `Cli -> Implementation -> Abstractions`

### Pool Optimization Algorithm

The `build-pools` command implements correlation-aware stochastic bin packing. Key components:

- **StatisticsService** — mean, percentile, Pearson correlation, overload fraction
- **PoolabilityService** — pairwise metrics: full correlation, peak correlation, peak overlap, bad-together score
- **PoolBuilder** — greedy construction sorted by P99 descending
- **LocalSearchOptimizer** — iterative move-based improvement to minimize total capacity

See [`docs/correlation-aware-pool-optimization.md`](docs/correlation-aware-pool-optimization.md) for details.
