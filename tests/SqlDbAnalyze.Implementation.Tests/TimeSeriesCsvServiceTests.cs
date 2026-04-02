using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class TimeSeriesCsvServiceTests : IDisposable
{
    private readonly TimeSeriesCsvService sut = new();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public TimeSeriesCsvServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task WriteAsync_ShouldCreateCsvFile_WhenTimeSeriesProvided()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test.csv");
        var ts = CreateSampleTimeSeries();

        // Act
        await sut.WriteAsync(ts, filePath, CancellationToken.None);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var lines = await File.ReadAllLinesAsync(filePath);
        lines.Length.Should().Be(4); // header + 3 data rows
        lines[0].Should().Contain("Timestamp");
        lines[0].Should().Contain("db1");
        lines[0].Should().Contain("db2");
    }

    [Fact]
    public async Task ReadAsync_ShouldRoundTrip_WhenWrittenThenRead()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "roundtrip.csv");
        var original = CreateSampleTimeSeries();
        await sut.WriteAsync(original, filePath, CancellationToken.None);

        // Act
        var result = await sut.ReadAsync(filePath, CancellationToken.None);

        // Assert
        result.Timestamps.Should().HaveCount(3);
        result.DatabaseValues.Should().ContainKey("db1");
        result.DatabaseValues.Should().ContainKey("db2");
        result.DatabaseValues["db1"][0].Should().BeApproximately(10.0, 0.01);
        result.DatabaseValues["db2"][2].Should().BeApproximately(60.0, 0.01);
    }

    [Fact]
    public async Task ReadAsync_ShouldThrow_WhenFileHasOnlyHeader()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "headeronly.csv");
        await File.WriteAllTextAsync(filePath, "Timestamp,db1\n");

        // Act
        var act = () => sut.ReadAsync(filePath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Merge_ShouldReturnEmpty_WhenNoSeriesProvided()
    {
        // Arrange
        IReadOnlyList<DtuTimeSeries> series = [];

        // Act
        var result = sut.Merge(series);

        // Assert
        result.Timestamps.Should().BeEmpty();
        result.DatabaseValues.Should().BeEmpty();
    }

    [Fact]
    public void Merge_ShouldReturnOriginal_WhenSingleSeriesProvided()
    {
        // Arrange
        var single = CreateSampleTimeSeries();

        // Act
        var result = sut.Merge([single]);

        // Assert
        result.Should().Be(single);
    }

    [Fact]
    public void Merge_ShouldCombineDatabases_WhenMultipleSeriesProvided()
    {
        // Arrange
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamps = new List<DateTimeOffset> { baseTime, baseTime.AddMinutes(5) };

        var series1 = new DtuTimeSeries(
            timestamps,
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [10.0, 20.0]
            });

        var series2 = new DtuTimeSeries(
            timestamps,
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db2"] = [30.0, 40.0]
            });

        // Act
        var result = sut.Merge([series1, series2]);

        // Assert
        result.DatabaseValues.Should().ContainKey("db1");
        result.DatabaseValues.Should().ContainKey("db2");
        result.Timestamps.Should().HaveCount(2);
    }

    private static DtuTimeSeries CreateSampleTimeSeries()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var timestamps = new List<DateTimeOffset>
        {
            baseTime,
            baseTime.AddMinutes(5),
            baseTime.AddMinutes(10)
        };

        return new DtuTimeSeries(
            timestamps,
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["db1"] = [10.0, 20.0, 30.0],
                ["db2"] = [40.0, 50.0, 60.0]
            });
    }
}
