namespace SqlDbAnalyze.Abstractions.Models;

public record DatabaseDtuSummary(
    string DatabaseName,
    double AverageDtuPercent,
    double PeakDtuPercent,
    int CurrentDtuLimit);
