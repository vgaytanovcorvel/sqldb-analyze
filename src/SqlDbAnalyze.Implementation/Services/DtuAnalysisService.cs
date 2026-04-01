using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class DtuAnalysisService : IDtuAnalysisService
{
    public virtual IReadOnlyList<HourlyDtuAggregate> AggregateByHour(IReadOnlyList<DtuMetric> metrics)
    {
        return metrics
            .GroupBy(m => new DateTimeOffset(
                m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day,
                m.Timestamp.Hour, 0, 0, m.Timestamp.Offset))
            .Select(g => new HourlyDtuAggregate(
                g.Key,
                g.Average(m => m.DtuPercentage),
                g.Max(m => m.DtuPercentage)))
            .OrderBy(a => a.Hour)
            .ToList();
    }

    public virtual DatabaseDtuSummary Summarize(
        string databaseName,
        IReadOnlyList<DtuMetric> metrics,
        int currentDtuLimit)
    {
        var averageDtu = metrics.Count > 0 ? metrics.Average(m => m.DtuPercentage) : 0;
        var peakDtu = metrics.Count > 0 ? metrics.Max(m => m.DtuPercentage) : 0;

        return new DatabaseDtuSummary(databaseName, averageDtu, peakDtu, currentDtuLimit);
    }

    public virtual ElasticPoolRecommendation Recommend(IReadOnlyList<DatabaseDtuSummary> summaries)
    {
        var totalAverageDtu = summaries.Sum(s => s.AverageDtuPercent / 100.0 * s.CurrentDtuLimit);

        var (tier, dtu) = totalAverageDtu switch
        {
            <= 50 => ("Basic", 50),
            <= 100 => ("Standard", 100),
            <= 200 => ("Standard", 200),
            <= 400 => ("Standard", 400),
            <= 800 => ("Standard", 800),
            <= 1200 => ("Standard", 1200),
            <= 1600 => ("Premium", 1600),
            _ => ("Premium", 2000)
        };

        return new ElasticPoolRecommendation(tier, dtu, totalAverageDtu, summaries);
    }
}
