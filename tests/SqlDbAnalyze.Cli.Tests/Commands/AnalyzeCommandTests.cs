using System.CommandLine;
using System.CommandLine.Parsing;
using FluentAssertions;
using SqlDbAnalyze.Cli.Commands;
using Xunit;

namespace SqlDbAnalyze.Cli.Tests.Commands;

public class AnalyzeCommandTests
{
    private readonly AnalyzeCommand sut = new();

    [Fact]
    public void Name_ShouldBeAnalyze_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var name = sut.Name;

        // Assert
        name.Should().Be("analyze");
    }

    [Fact]
    public void Description_ShouldContainDtu_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var description = sut.Description;

        // Assert
        description.Should().NotBeNullOrWhiteSpace();
        description.Should().Contain("DTU");
    }

    [Fact]
    public void Arguments_ShouldContainServerName_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var argument = sut.Arguments
            .FirstOrDefault(a => a.Name == "server-name");

        // Assert
        argument.Should().NotBeNull();
        argument!.ValueType.Should().Be(typeof(string));
    }

    [Fact]
    public void SubscriptionOption_ShouldBeRequired_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--subscription"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void SubscriptionOption_ShouldHaveShortAlias_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--subscription"));

        // Assert
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("-s");
    }

    [Fact]
    public void ResourceGroupOption_ShouldBeRequired_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--resource-group"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void ResourceGroupOption_ShouldHaveShortAlias_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--resource-group"));

        // Assert
        option.Should().NotBeNull();
        option!.Aliases.Should().Contain("-g");
    }

    [Fact]
    public void HoursOption_ShouldNotBeRequired_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--hours"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void HoursOption_ShouldDefaultTo24_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-id --resource-group rg-name");
        var hoursOption = sut.Options.First(o => o.Aliases.Contains("--hours")) as Option<int>;

        // Assert
        hoursOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(hoursOption!);
        value.Should().Be(24);
    }

    [Fact]
    public void Parse_ShouldExtractServerName_WhenValidArgumentsProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-123 --resource-group my-rg");
        var serverArg = sut.Arguments.First(a => a.Name == "server-name") as Argument<string>;

        // Assert
        serverArg.Should().NotBeNull();
        var value = parseResult.GetValueForArgument(serverArg!);
        value.Should().Be("my-server");
    }

    [Fact]
    public void Parse_ShouldExtractSubscription_WhenValidArgumentsProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-123 --resource-group my-rg");
        var subscriptionOption = sut.Options.First(o => o.Aliases.Contains("--subscription")) as Option<string>;

        // Assert
        subscriptionOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(subscriptionOption!);
        value.Should().Be("sub-123");
    }

    [Fact]
    public void Parse_ShouldExtractResourceGroup_WhenValidArgumentsProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-123 --resource-group my-rg");
        var rgOption = sut.Options.First(o => o.Aliases.Contains("--resource-group")) as Option<string>;

        // Assert
        rgOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(rgOption!);
        value.Should().Be("my-rg");
    }

    [Fact]
    public void Parse_ShouldExtractCustomHours_WhenHoursOptionProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-123 --resource-group my-rg --hours 48");
        var hoursOption = sut.Options.First(o => o.Aliases.Contains("--hours")) as Option<int>;

        // Assert
        hoursOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(hoursOption!);
        value.Should().Be(48);
    }

    [Fact]
    public void Parse_ShouldExtractSubscription_WhenShortAliasUsed()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server -s sub-123 --resource-group my-rg");
        var subscriptionOption = sut.Options.First(o => o.Aliases.Contains("--subscription")) as Option<string>;

        // Assert
        subscriptionOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(subscriptionOption!);
        value.Should().Be("sub-123");
    }

    [Fact]
    public void Parse_ShouldExtractResourceGroup_WhenShortAliasUsed()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server -s sub-123 -g my-rg");
        var rgOption = sut.Options.First(o => o.Aliases.Contains("--resource-group")) as Option<string>;

        // Assert
        rgOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(rgOption!);
        value.Should().Be("my-rg");
    }

    [Fact]
    public void Parse_ShouldProduceErrors_WhenSubscriptionMissing()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --resource-group my-rg");

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
        parseResult.Errors.Should().Contain(e =>
            e.Message.Contains("--subscription") || e.Message.Contains("-s"));
    }

    [Fact]
    public void Parse_ShouldProduceErrors_WhenResourceGroupMissing()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-123");

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
        parseResult.Errors.Should().Contain(e =>
            e.Message.Contains("--resource-group") || e.Message.Contains("-g"));
    }

    [Fact]
    public void WindowStartOption_ShouldNotBeRequired_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--window-start"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void WindowEndOption_ShouldNotBeRequired_WhenCommandIsConstructed()
    {
        // Arrange
        // (sut created via field initializer)

        // Act
        var option = sut.Options
            .FirstOrDefault(o => o.Aliases.Contains("--window-end"));

        // Assert
        option.Should().NotBeNull();
        option!.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void WindowTimezoneOption_ShouldDefaultToEastern_WhenNotProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-id --resource-group rg-name");
        var tzOption = sut.Options.First(o => o.Aliases.Contains("--window-timezone")) as Option<string>;

        // Assert
        tzOption.Should().NotBeNull();
        var value = parseResult.GetValueForOption(tzOption!);
        value.Should().Be("Eastern Standard Time");
    }

    [Fact]
    public void Parse_ShouldExtractWindowStart_WhenWindowStartProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse(
            "my-server --subscription sub-123 --resource-group my-rg --window-start 09:00");
        var option = sut.Options.First(o => o.Aliases.Contains("--window-start")) as Option<string?>;

        // Assert
        option.Should().NotBeNull();
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be("09:00");
    }

    [Fact]
    public void Parse_ShouldExtractWindowEnd_WhenWindowEndProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse(
            "my-server --subscription sub-123 --resource-group my-rg --window-end 17:00");
        var option = sut.Options.First(o => o.Aliases.Contains("--window-end")) as Option<string?>;

        // Assert
        option.Should().NotBeNull();
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be("17:00");
    }

    [Fact]
    public void Parse_ShouldExtractWindowTimezone_WhenCustomTimezoneProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse(
            "my-server --subscription sub-123 --resource-group my-rg --window-timezone \"Pacific Standard Time\"");
        var option = sut.Options.First(o => o.Aliases.Contains("--window-timezone")) as Option<string>;

        // Assert
        option.Should().NotBeNull();
        var value = parseResult.GetValueForOption(option!);
        value.Should().Be("Pacific Standard Time");
    }

    [Fact]
    public void Parse_ShouldProduceNoErrors_WhenAllWindowOptionsProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse(
            "my-server --subscription sub-123 --resource-group my-rg --window-start 09:00 --window-end 17:00 --window-timezone \"Eastern Standard Time\"");

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ShouldProduceErrors_WhenServerNameMissing()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("--subscription sub-123 --resource-group my-rg");

        // Assert
        parseResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_ShouldProduceNoErrors_WhenAllRequiredArgumentsProvided()
    {
        // Arrange
        var parser = new Parser(sut);

        // Act
        var parseResult = parser.Parse("my-server --subscription sub-123 --resource-group my-rg");

        // Assert
        parseResult.Errors.Should().BeEmpty();
    }
}
