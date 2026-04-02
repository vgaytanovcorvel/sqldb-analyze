namespace SqlDbAnalyze.Abstractions.Models;

public record DtuTimeSeries(
    IReadOnlyList<DateTimeOffset> Timestamps,
    IReadOnlyDictionary<string, IReadOnlyList<double>> DatabaseValues);
