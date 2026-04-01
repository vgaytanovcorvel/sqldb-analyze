namespace SqlDbAnalyze.Abstractions.Models;

public record HourlyDtuAggregate(
    DateTimeOffset Hour,
    double AverageDtuPercent,
    double MaxDtuPercent);
