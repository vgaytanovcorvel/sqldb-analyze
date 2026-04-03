using System.CommandLine;
using System.CommandLine.IO;
using FluentAssertions;
using SqlDbAnalyze.Cli.Commands;
using Xunit;

namespace SqlDbAnalyze.Cli.Tests.Commands;

public class TimeWindowParserTests
{
    private readonly TestConsole _console = new();

    [Fact]
    public void Parse_ShouldReturnNull_WhenBothStartAndEndAreNull()
    {
        // Act
        var result = TimeWindowParser.Parse(null, null, "Eastern Standard Time", _console);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenOnlyStartProvided()
    {
        // Act
        var result = TimeWindowParser.Parse("09:00", null, "Eastern Standard Time", _console);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenOnlyEndProvided()
    {
        // Act
        var result = TimeWindowParser.Parse(null, "17:00", "Eastern Standard Time", _console);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnWindow_WhenBothStartAndEndProvided()
    {
        // Act
        var result = TimeWindowParser.Parse("09:00", "17:00", "Eastern Standard Time", _console);

        // Assert
        result.Should().NotBeNull();
        result!.StartTime.Should().Be(new TimeOnly(9, 0));
        result.EndTime.Should().Be(new TimeOnly(17, 0));
        result.TimeZoneId.Should().Be("Eastern Standard Time");
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenStartTimeIsInvalid()
    {
        // Act
        var result = TimeWindowParser.Parse("not-a-time", "17:00", "Eastern Standard Time", _console);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenEndTimeIsInvalid()
    {
        // Act
        var result = TimeWindowParser.Parse("09:00", "invalid", "Eastern Standard Time", _console);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNull_WhenTimezoneIsInvalid()
    {
        // Act
        var result = TimeWindowParser.Parse("09:00", "17:00", "Fake/Timezone", _console);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnWindow_WhenOvernightWindowProvided()
    {
        // Act
        var result = TimeWindowParser.Parse("22:00", "06:00", "Eastern Standard Time", _console);

        // Assert
        result.Should().NotBeNull();
        result!.StartTime.Should().Be(new TimeOnly(22, 0));
        result.EndTime.Should().Be(new TimeOnly(6, 0));
    }

    [Fact]
    public void Parse_ShouldReturnWindow_WhenMidnightBoundaryUsed()
    {
        // Act
        var result = TimeWindowParser.Parse("00:00", "23:59", "Eastern Standard Time", _console);

        // Assert
        result.Should().NotBeNull();
        result!.StartTime.Should().Be(new TimeOnly(0, 0));
        result.EndTime.Should().Be(new TimeOnly(23, 59));
    }
}
