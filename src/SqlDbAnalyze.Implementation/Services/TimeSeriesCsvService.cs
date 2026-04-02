using System.Globalization;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class TimeSeriesCsvService : ITimeSeriesCsvService
{
    public virtual async Task WriteAsync(
        DtuTimeSeries timeSeries,
        string filePath,
        CancellationToken cancellationToken)
    {
        var dbNames = timeSeries.DatabaseValues.Keys.OrderBy(n => n).ToList();

        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync(BuildHeaderLine(dbNames));

        for (var i = 0; i < timeSeries.Timestamps.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(BuildDataLine(timeSeries, dbNames, i));
        }
    }

    public virtual async Task<DtuTimeSeries> ReadAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length < 2)
            throw new InvalidOperationException("CSV file must contain a header and at least one data row.");

        var dbNames = ParseHeader(lines[0]);
        var timestamps = new List<DateTimeOffset>();
        var columns = dbNames.Select(_ => new List<double>()).ToArray();

        for (var i = 1; i < lines.Length; i++)
            ParseDataLine(lines[i], dbNames.Length, timestamps, columns);

        return BuildTimeSeries(dbNames, timestamps, columns);
    }

    public virtual DtuTimeSeries Merge(IReadOnlyList<DtuTimeSeries> series)
    {
        if (series.Count == 0) return new DtuTimeSeries([], new Dictionary<string, IReadOnlyList<double>>());
        if (series.Count == 1) return series[0];

        var allTimestamps = series
            .SelectMany(s => s.Timestamps)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var merged = new Dictionary<string, IReadOnlyList<double>>();
        foreach (var ts in series)
            MergeOneSeries(ts, allTimestamps, merged);

        return new DtuTimeSeries(allTimestamps, merged);
    }

    private static string BuildHeaderLine(List<string> dbNames)
    {
        return "Timestamp," + string.Join(",", dbNames);
    }

    private static string BuildDataLine(DtuTimeSeries timeSeries, List<string> dbNames, int index)
    {
        var timestamp = timeSeries.Timestamps[index].ToString("o", CultureInfo.InvariantCulture);
        var values = dbNames.Select(n => timeSeries.DatabaseValues[n][index].ToString("F2", CultureInfo.InvariantCulture));
        return timestamp + "," + string.Join(",", values);
    }

    private static string[] ParseHeader(string headerLine)
    {
        var parts = headerLine.Split(',');
        return parts.Skip(1).Select(h => h.Trim()).ToArray();
    }

    private static void ParseDataLine(
        string line,
        int expectedColumns,
        List<DateTimeOffset> timestamps,
        List<double>[] columns)
    {
        var parts = line.Split(',');
        timestamps.Add(DateTimeOffset.Parse(parts[0].Trim(), CultureInfo.InvariantCulture));

        for (var j = 0; j < expectedColumns; j++)
        {
            var value = j + 1 < parts.Length
                ? double.TryParse(parts[j + 1].Trim(), CultureInfo.InvariantCulture, out var v) ? v : 0
                : 0;
            columns[j].Add(value);
        }
    }

    private static DtuTimeSeries BuildTimeSeries(
        string[] dbNames,
        List<DateTimeOffset> timestamps,
        List<double>[] columns)
    {
        var dict = new Dictionary<string, IReadOnlyList<double>>();
        for (var i = 0; i < dbNames.Length; i++)
            dict[dbNames[i]] = columns[i];

        return new DtuTimeSeries(timestamps, dict);
    }

    private static void MergeOneSeries(
        DtuTimeSeries source,
        List<DateTimeOffset> targetTimestamps,
        Dictionary<string, IReadOnlyList<double>> merged)
    {
        var sourceIndex = source.Timestamps
            .Select((t, i) => (t, i))
            .ToDictionary(x => x.t, x => x.i);

        foreach (var (dbName, sourceValues) in source.DatabaseValues)
        {
            var aligned = targetTimestamps
                .Select(t => sourceIndex.TryGetValue(t, out var idx) ? sourceValues[idx] : 0)
                .ToList();

            merged[dbName] = aligned;
        }
    }
}
