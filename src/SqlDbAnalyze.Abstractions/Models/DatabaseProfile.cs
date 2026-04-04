namespace SqlDbAnalyze.Abstractions.Models;

public record DatabaseProfile(
    string DatabaseName,
    IReadOnlyList<double> DtuValues,
    double Mean,
    double P95,
    double P99,
    double Peak,
    double StdDev = 0,
    double ActiveFraction = 0,
    bool IsLowSignal = false);
