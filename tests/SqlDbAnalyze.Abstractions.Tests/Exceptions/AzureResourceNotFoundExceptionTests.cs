using FluentAssertions;
using SqlDbAnalyze.Abstractions.Exceptions;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Exceptions;

public class AzureResourceNotFoundExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessage_When_MessageProvided()
    {
        // Arrange
        var message = "Resource not found in subscription.";

        // Act
        var exception = new AzureResourceNotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_Should_InheritFromException_When_Created()
    {
        // Arrange
        var message = "test";

        // Act
        var exception = new AzureResourceNotFoundException(message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_Should_HaveNullInnerException_When_OnlyMessageProvided()
    {
        // Arrange
        var message = "test";

        // Act
        var exception = new AzureResourceNotFoundException(message);

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_Should_PreserveExactMessage_When_SpecialCharactersUsed()
    {
        // Arrange
        var message = "Server 'my-server' not found in resource group 'rg-prod' (subscription: abc-123).";

        // Act
        var exception = new AzureResourceNotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_Should_UseDefaultMessage_When_NullMessageProvided()
    {
        // Arrange
        string? message = null;

        // Act
        var exception = new AzureResourceNotFoundException(message!);

        // Assert
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_Should_BeThrowable_When_Used()
    {
        // Arrange
        var message = "Database not found.";

        // Act
        Action act = () => throw new AzureResourceNotFoundException(message);

        // Assert
        act.Should().Throw<AzureResourceNotFoundException>()
            .WithMessage(message);
    }

    [Fact]
    public void Constructor_Should_BeCatchableAsException_When_Thrown()
    {
        // Arrange
        var message = "Resource missing.";

        // Act
        Action act = () => throw new AzureResourceNotFoundException(message);

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage(message);
    }

    [Fact]
    public void Constructor_Should_AcceptEmptyMessage_When_EmptyStringProvided()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var exception = new AzureResourceNotFoundException(message);

        // Assert
        exception.Message.Should().BeEmpty();
    }
}
