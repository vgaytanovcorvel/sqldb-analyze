namespace SqlDbAnalyze.Abstractions.Models;

public record DatabaseProfile(
    string DatabaseName,
    IReadOnlyList<double> DtuValues,
    double Mean,
    double P95,
    double P99,
    double Peak);
