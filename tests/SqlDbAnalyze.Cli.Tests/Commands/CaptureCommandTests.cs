using System.CommandLine;
using System.CommandLine.Parsing;
using FluentAssertions;
using SqlDbAnalyze.Cli.Commands;
using Xunit;

namespace SqlDbAnalyze.Cli.Tests.Commands;

public class CaptureCommandTests
{
    private readonly CaptureCommand sut = new();

    [Fact]
    public void Name_ShouldBeCapture_WhenCommandIsConstructed()
    {
        // Arrange / Act
        var name = sut.Name;

        // Assert
        name.Should().Be("capture");
    }

    [Fact]
    public void Arguments_ShouldContainServerName_WhenCommandIsConstructed()
    {
        // Act
        var argument = sut.Arguments.FirstOrDefault(a => a.Name == "server-name");

        // Assert
        argument.Should().NotBeNull();
    }

    [Fact]
    public void SubscriptionOption_ShouldBeRequired_WhenCommandIsConstructed()
    {
        // Act
        var option = sut.Options.FirstOrDefault(o => o.Aliases.Contains("--subscription"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeTrue();
        option.Aliases.Should().Contain("-s");
    }

    [Fact]
    public void ResourceGroupOption_ShouldBeRequired_WhenCommandIsConstructed()
    {
        // Act
        var option = sut.Options.FirstOrDefault(o => o.Aliases.Contains("--resource-group"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeTrue();
        option.Aliases.Should().Contain("-g");
    }

    [Fact]
    public void OutputOption_ShouldDefaultToCsv_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server -s sub -g rg");
        var option = sut.Options.First(o => o.Aliases.Contains("--output")) as Option<string>;

        // Assert
        option.Should().NotBeNull();
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be("dtu-metrics.csv");
    }

    [Fact]
    public void OutputOption_ShouldHaveShortAlias_WhenCommandIsConstructed()
    {
        // Act
        var option = sut.Options.FirstOrDefault(o => o.Aliases.Contains("--output"));

        // Assert
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("-o");
    }

    [Fact]
    public void HoursOption_ShouldDefaultTo24_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server -s sub -g rg");
        var option = sut.Options.First(o => o.Aliases.Contains("--hours")) as Option<int>;

        // Assert
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be(24);
    }

    [Fact]
    public void Parse_ShouldProduceNoErrors_WhenAllRequiredArgsProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server -s sub-123 -g my-rg");

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldProduceErrors_WhenServerNameMissing()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("-s sub-123 -g my-rg");

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
    }
}
