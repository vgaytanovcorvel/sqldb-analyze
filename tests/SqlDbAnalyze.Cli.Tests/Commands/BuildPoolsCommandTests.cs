using System.CommandLine;
using System.CommandLine.Parsing;
using FluentAssertions;
using SqlDbAnalyze.Cli.Commands;
using Xunit;

namespace SqlDbAnalyze.Cli.Tests.Commands;

public class BuildPoolsCommandTests
{
    private readonly BuildPoolsCommand sut = new();

    [Fact]
    public void Name_ShouldBeBuildPools_WhenCommandIsConstructed()
    {
        // Act
        var name = sut.Name;

        // Assert
        name.Should().Be("build-pools");
    }

    [Fact]
    public void Arguments_ShouldContainCsvFiles_WhenCommandIsConstructed()
    {
        // Act
        var argument = sut.Arguments.FirstOrDefault(a => a.Name == "csv-files");

        // Assert
        argument.Should().NotBeNull();
    }

    [Fact]
    public void TargetPercentileOption_ShouldDefaultTo099_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("data.csv");
        var option = sut.Options.First(o => o.Aliases.Contains("--target-percentile")) as Option<double>;

        // Assert
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be(0.99);
    }

    [Fact]
    public void SafetyFactorOption_ShouldDefaultTo110_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("data.csv");
        var option = sut.Options.First(o => o.Aliases.Contains("--safety-factor")) as Option<double>;

        // Assert
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be(1.10);
    }

    [Fact]
    public void MaxDbsPerPoolOption_ShouldDefaultTo50_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("data.csv");
        var option = sut.Options.First(o => o.Aliases.Contains("--max-dbs-per-pool")) as Option<int>;

        // Assert
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be(50);
    }

    [Fact]
    public void MaxSearchPassesOption_ShouldDefaultTo10_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("data.csv");
        var option = sut.Options.First(o => o.Aliases.Contains("--max-search-passes")) as Option<int>;

        // Assert
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be(10);
    }

    [Fact]
    public void Parse_ShouldProduceNoErrors_WhenCsvFileProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("data.csv");

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldAcceptMultipleCsvFiles_WhenMultipleProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("data1.csv data2.csv data3.csv");
        var argument = sut.Arguments.First(a => a.Name == "csv-files") as Argument<string[]>;

        // Assert
        parseResult.Errors.Should().BeEmpty();
        var value = parseResult.GetValueForArgument(argument!);
        value.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_ShouldExtractCustomOptions_WhenAllProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse(
            "data.csv --target-percentile 0.95 --safety-factor 1.20 --max-dbs-per-pool 20 --peak-threshold 0.85");

        // Assert
        parseResult.Errors.Should().BeEmpty();

        var tp = sut.Options.First(o => o.Aliases.Contains("--target-percentile")) as Option<double>;
        parseResult.GetValueForOption(tp!).Should().Be(0.95);

        var sf = sut.Options.First(o => o.Aliases.Contains("--safety-factor")) as Option<double>;
        parseResult.GetValueForOption(sf!).Should().Be(1.20);

        var maxDb = sut.Options.First(o => o.Aliases.Contains("--max-dbs-per-pool")) as Option<int>;
        parseResult.GetValueForOption(maxDb!).Should().Be(20);
    }

    [Fact]
    public void Parse_ShouldProduceErrors_WhenNoCsvFileProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("--target-percentile 0.95");

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
    }
}
