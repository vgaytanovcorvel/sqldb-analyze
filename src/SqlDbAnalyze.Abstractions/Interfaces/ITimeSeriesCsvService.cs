using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface ITimeSeriesCsvService
{
    Task<DtuTimeSeries> ReadAsync(string filePath, CancellationToken cancellationToken);

    Task WriteAsync(DtuTimeSeries timeSeries, string filePath, CancellationToken cancellationToken);

    DtuTimeSeries Merge(IReadOnlyList<DtuTimeSeries> series);
}
