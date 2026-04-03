namespace SqlDbAnalyze.Abstractions.Models;

public record PoolSimulationRequest(
    IReadOnlyList<string> DatabaseNames,
    IReadOnlyDictionary<string, int> DtuLimits);
