using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class AnalysisTimeWindowTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties_WhenValidValuesProvided()
    {
        // Arrange
        var startTime = new TimeOnly(9, 0);
        var endTime = new TimeOnly(17, 0);
        var timeZoneId = "Eastern Standard Time";

        // Act
        var window = new AnalysisTimeWindow(startTime, endTime, timeZoneId);

        // Assert
        window.StartTime.Should().Be(startTime);
        window.EndTime.Should().Be(endTime);
        window.TimeZoneId.Should().Be(timeZoneId);
    }

    [Fact]
    public void Equality_ShouldBeTrue_WhenPropertiesAreEqual()
    {
        // Arrange
        var window1 = new AnalysisTimeWindow(new TimeOnly(9, 0), new TimeOnly(17, 0), "Eastern Standard Time");
        var window2 = new AnalysisTimeWindow(new TimeOnly(9, 0), new TimeOnly(17, 0), "Eastern Standard Time");

        // Act & Assert
        window1.Should().Be(window2);
    }

    [Fact]
    public void Equality_ShouldBeFalse_WhenStartTimeDiffers()
    {
        // Arrange
        var window1 = new AnalysisTimeWindow(new TimeOnly(9, 0), new TimeOnly(17, 0), "Eastern Standard Time");
        var window2 = new AnalysisTimeWindow(new TimeOnly(10, 0), new TimeOnly(17, 0), "Eastern Standard Time");

        // Act & Assert
        window1.Should().NotBe(window2);
    }

    [Fact]
    public void With_ShouldCreateModifiedCopy_WhenEndTimeChanged()
    {
        // Arrange
        var original = new AnalysisTimeWindow(new TimeOnly(9, 0), new TimeOnly(17, 0), "Eastern Standard Time");

        // Act
        var modified = original with { EndTime = new TimeOnly(20, 0) };

        // Assert
        modified.StartTime.Should().Be(new TimeOnly(9, 0));
        modified.EndTime.Should().Be(new TimeOnly(20, 0));
        modified.TimeZoneId.Should().Be("Eastern Standard Time");
        original.EndTime.Should().Be(new TimeOnly(17, 0));
    }
}
