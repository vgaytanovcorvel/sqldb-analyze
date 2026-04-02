using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Cli.Commands;

public class BuildPoolsCommand : Command
{
    private readonly Argument<string[]> _csvFilesArgument = new(
        "csv-files",
        "One or more CSV files with DTU time series data")
    { Arity = new ArgumentArity(1, 100) };

    private readonly Option<double> _targetPercentileOption = new(
        ["--target-percentile"],
        getDefaultValue: () => 0.99,
        "Target percentile for pool sizing (e.g., 0.99)");

    private readonly Option<double> _safetyFactorOption = new(
        ["--safety-factor"],
        getDefaultValue: () => 1.10,
        "Safety factor multiplier for pool capacity (e.g., 1.10 for 10%)");

    private readonly Option<double> _maxOverloadOption = new(
        ["--max-overload"],
        getDefaultValue: () => 0.001,
        "Maximum acceptable overload fraction (e.g., 0.001 for 0.1%)");

    private readonly Option<int> _maxDbsPerPoolOption = new(
        ["--max-dbs-per-pool"],
        getDefaultValue: () => 50,
        "Maximum number of databases per pool");

    private readonly Option<double> _peakThresholdOption = new(
        ["--peak-threshold"],
        getDefaultValue: () => 0.90,
        "Percentile threshold for peak detection (e.g., 0.90)");

    private readonly Option<double?> _maxPoolCapacityOption = new(
        ["--max-pool-capacity"],
        "Maximum pool capacity in DTUs (optional hard cap)");

    private readonly Option<string[]?> _isolateOption = new(
        ["--isolate"],
        "Database names to isolate in their own pools (comma-separated)");

    private readonly Option<int> _maxSearchPassesOption = new(
        ["--max-search-passes"],
        getDefaultValue: () => 10,
        "Maximum local search improvement passes");

    public BuildPoolsCommand() : base("build-pools", "Build optimal elastic pool assignments from CSV time series data")
    {
        AddArgument(_csvFilesArgument);
        AddOption(_targetPercentileOption);
        AddOption(_safetyFactorOption);
        AddOption(_maxOverloadOption);
        AddOption(_maxDbsPerPoolOption);
        AddOption(_peakThresholdOption);
        AddOption(_maxPoolCapacityOption);
        AddOption(_isolateOption);
        AddOption(_maxSearchPassesOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var host = context.BindingContext.GetRequiredService<IHost>();
            var csvService = host.Services.GetRequiredService<ITimeSeriesCsvService>();
            var poolabilityService = host.Services.GetRequiredService<IPoolabilityService>();
            var poolBuilder = host.Services.GetRequiredService<IPoolBuilder>();
            var localSearch = host.Services.GetRequiredService<ILocalSearchOptimizer>();

            var csvFiles = context.ParseResult.GetValueForArgument(_csvFilesArgument);
            var ct = context.GetCancellationToken();
            var console = context.Console;

            var options = BuildOptions(context);
            var timeSeries = await LoadTimeSeries(csvFiles, csvService, ct);
            var profiles = poolabilityService.BuildProfiles(timeSeries);

            console.WriteLine($"Loaded {profiles.Count} databases with {timeSeries.Timestamps.Count} time points.");
            console.WriteLine("");

            var initial = poolBuilder.BuildPools(profiles, options);
            var optimized = localSearch.Improve(initial, profiles, options);

            WriteResults(console, optimized);
        });
    }

    private PoolOptimizerOptions BuildOptions(InvocationContext context)
    {
        return new PoolOptimizerOptions(
            TargetPercentile: context.ParseResult.GetValueForOption(_targetPercentileOption),
            SafetyFactor: context.ParseResult.GetValueForOption(_safetyFactorOption),
            MaxOverloadFraction: context.ParseResult.GetValueForOption(_maxOverloadOption),
            MaxDatabasesPerPool: context.ParseResult.GetValueForOption(_maxDbsPerPoolOption),
            PeakThreshold: context.ParseResult.GetValueForOption(_peakThresholdOption),
            MaxPoolCapacity: context.ParseResult.GetValueForOption(_maxPoolCapacityOption),
            IsolateDatabases: context.ParseResult.GetValueForOption(_isolateOption),
            MaxSearchPasses: context.ParseResult.GetValueForOption(_maxSearchPassesOption));
    }

    private static async Task<DtuTimeSeries> LoadTimeSeries(
        string[] csvFiles,
        ITimeSeriesCsvService csvService,
        CancellationToken ct)
    {
        if (csvFiles.Length == 1)
            return await csvService.ReadAsync(csvFiles[0], ct);

        var allSeries = new List<DtuTimeSeries>();
        foreach (var file in csvFiles)
            allSeries.Add(await csvService.ReadAsync(file, ct));

        return csvService.Merge(allSeries);
    }

    private static void WriteResults(IConsole console, PoolOptimizationResult result)
    {
        console.WriteLine($"Recommended {result.Pools.Count} pool(s), total capacity: {result.TotalRequiredCapacity:F1} DTU");
        console.WriteLine("");

        WritePoolTree(console, result);
        WriteIsolated(console, result);
        WriteLegend(console);
    }

    private static void WritePoolTree(IConsole console, PoolOptimizationResult result)
    {
        foreach (var pool in result.Pools)
        {
            console.WriteLine(
                $"Pool {pool.PoolIndex} -- {pool.RecommendedCapacity:F1} DTU capacity, " +
                $"{pool.DatabaseNames.Count} database(s)");
            console.WriteLine(
                $"|  P95: {pool.P95Load:F1}   P99: {pool.P99Load:F1}   " +
                $"Peak: {pool.PeakLoad:F1}   " +
                $"Divers: {pool.DiversificationRatio:F2}   " +
                $"Overload: {pool.OverloadFraction:F4}%");
            console.WriteLine("|");

            WriteTreeItems(console, pool.DatabaseNames);
            console.WriteLine("");
        }
    }

    private static void WriteIsolated(IConsole console, PoolOptimizationResult result)
    {
        if (result.IsolatedDatabases.Count == 0) return;

        console.WriteLine("Isolated databases:");
        WriteTreeItems(console, result.IsolatedDatabases);
        console.WriteLine("");
    }

    private static void WriteTreeItems(IConsole console, IReadOnlyList<string> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var isLast = i == items.Count - 1;
            console.WriteLine($"{(isLast ? "└── " : "├── ")}{items[i]}");
        }
    }

    private static void WriteLegend(IConsole console)
    {
        console.WriteLine("Legend:");
        console.WriteLine("  Capacity  Recommended pool size (target percentile x safety factor)");
        console.WriteLine("  P95/P99   95th and 99th percentile of combined pool load (DTU)");
        console.WriteLine("  Peak      Maximum observed combined load (DTU)");
        console.WriteLine("  Divers    Diversification ratio: sum of individual P99 / pooled P99 (higher = more efficient pooling)");
        console.WriteLine("  Overload  Fraction of time combined load exceeds pool capacity");
        console.WriteLine("");
    }
}
