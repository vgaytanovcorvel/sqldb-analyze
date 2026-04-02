using System.CommandLine;
using System.CommandLine.Parsing;
using FluentAssertions;
using SqlDbAnalyze.Cli.Commands;
using Xunit;

namespace SqlDbAnalyze.Cli.Tests;

public class RootCommandTests
{
    private readonly RootCommand sut = RootCommandFactory.Create();

    [Fact]
    public void Description_ShouldContainToolName_WhenCommandIsCreated()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var description = sut.Description;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
        description.Should().Contain("sqldb-analyze");
    }

    [Fact]
    public void Subcommands_ShouldContainAnalyzeCommand_WhenCommandIsCreated()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var analyzeCommand = sut.Subcommands
            .FirstOrDefault(c => c.Name == "analyze");

        // Assert
        analyzeCommand.Should().NotBeNull();
        analyzeCommand.Should().BeOfType<AnalyzeCommand>();
    }

    [Fact]
    public void Options_ShouldContainVerboseGlobalOption_WhenCommandIsCreated()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var verboseOption = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--verbose"));

        // Assert
        verboseOption.Should().NotBeNull();
        verboseOption!.Aliases.Should().Contain("-v");
    }

    [Fact]
    public void Parse_ShouldRecognizeVerboseFlag_WhenProvidedBeforeSubcommand()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("--verbose analyze my-server --subscription sub-123 --resource-group my-rg");
        var verboseOption = sut.Options.First(o => o.Aliases.Contains("--verbose")) as Option<bool>;

        // Assert
        verboseOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(verboseOption!);
        value.Should().BeTrue();
    }

    [Fact]
    public void Parse_ShouldRecognizeVerboseFlag_WhenProvidedAfterSubcommand()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("analyze my-server --subscription sub-123 --resource-group my-rg --verbose");
        var verboseOption = sut.Options.First(o => o.Aliases.Contains("--verbose")) as Option<bool>;

        // Assert
        verboseOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(verboseOption!);
        value.Should().BeTrue();
    }

    [Fact]
    public void Parse_ShouldDefaultVerboseToFalse_WhenFlagNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("analyze my-server --subscription sub-123 --resource-group my-rg");
        var verboseOption = sut.Options.First(o => o.Aliases.Contains("--verbose")) as Option<bool>;

        // Assert
        verboseOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(verboseOption!);
        value.Should().BeFalse();
    }

    [Fact]
    public void Parse_ShouldProduceNoErrors_WhenValidFullCommandProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("analyze my-server --subscription sub-123 --resource-group my-rg --hours 12");

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldProduceErrors_WhenUnknownSubcommandProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("unknown-command");

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
    }
}
