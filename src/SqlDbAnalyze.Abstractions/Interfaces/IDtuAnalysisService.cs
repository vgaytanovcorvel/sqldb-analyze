using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IDtuAnalysisService
{
    IReadOnlyList<HourlyDtuAggregate> AggregateByHour(IReadOnlyList<DtuMetric> metrics);

    DatabaseDtuSummary Summarize(
        string databaseName,
        IReadOnlyList<DtuMetric> metrics,
        int currentDtuLimit);

    ElasticPoolRecommendation Recommend(IReadOnlyList<DatabaseDtuSummary> summaries);
}
