namespace SqlDbAnalyze.Abstractions.Models;

public record AnalysisTimeWindow(
    TimeOnly StartTime,
    TimeOnly EndTime,
    string TimeZoneId);
