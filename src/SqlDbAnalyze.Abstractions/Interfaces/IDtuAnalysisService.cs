using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IDtuAnalysisService
{
    IReadOnlyList<DtuMetric> FilterByTimeWindow(
        IReadOnlyList<DtuMetric> metrics,
        AnalysisTimeWindow timeWindow);

    IReadOnlyList<HourlyDtuAggregate> AggregateByHour(IReadOnlyList<DtuMetric> metrics);

    DatabaseDtuSummary Summarize(
        string databaseName,
        IReadOnlyList<DtuMetric> metrics,
        int currentDtuLimit);

    ElasticPoolRecommendation Recommend(IReadOnlyList<DatabaseDtuSummary> summaries);
}
