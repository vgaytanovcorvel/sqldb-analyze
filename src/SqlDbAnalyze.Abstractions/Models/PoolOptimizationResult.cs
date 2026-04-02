namespace SqlDbAnalyze.Abstractions.Models;

public record PoolOptimizationResult(
    IReadOnlyList<PoolAssignment> Pools,
    double TotalRequiredCapacity,
    IReadOnlyList<string> IsolatedDatabases);
