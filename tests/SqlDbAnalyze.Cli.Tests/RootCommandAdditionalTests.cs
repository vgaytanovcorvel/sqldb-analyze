using System.CommandLine;
using FluentAssertions;
using SqlDbAnalyze.Cli.Commands;
using Xunit;

namespace SqlDbAnalyze.Cli.Tests;

public class RootCommandAdditionalTests
{
    private readonly RootCommand sut = RootCommandFactory.Create();

    [Fact]
    public void Subcommands_ShouldContainCaptureCommand_WhenCommandIsCreated()
    {
        // Act
        var captureCommand = sut.Subcommands.FirstOrDefault(c => c.Name == "capture");

        // Assert
        captureCommand.Should().NotBeNull();
        captureCommand.Should().BeOfType<CaptureCommand>();
    }

    [Fact]
    public void Subcommands_ShouldContainBuildPoolsCommand_WhenCommandIsCreated()
    {
        // Act
        var buildPoolsCommand = sut.Subcommands.FirstOrDefault(c => c.Name == "build-pools");

        // Assert
        buildPoolsCommand.Should().NotBeNull();
        buildPoolsCommand.Should().BeOfType<BuildPoolsCommand>();
    }

    [Fact]
    public void Subcommands_ShouldHaveThreeCommands_WhenCommandIsCreated()
    {
        // Act
        var count = sut.Subcommands.Count;

        // Assert
        count.Should().Be(3);
    }
}
