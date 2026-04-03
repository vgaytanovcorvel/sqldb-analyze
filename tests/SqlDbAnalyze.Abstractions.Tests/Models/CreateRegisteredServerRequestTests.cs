using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class CreateRegisteredServerRequestTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange & Act
        var request = new CreateRegisteredServerRequest("Prod", "sub-123", "rg-prod", "sql-prod");

        // Assert
        request.Name.Should().Be("Prod");
        request.SubscriptionId.Should().Be("sub-123");
        request.ResourceGroupName.Should().Be("rg-prod");
        request.ServerName.Should().Be("sql-prod");
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var req1 = new CreateRegisteredServerRequest("Name", "sub", "rg", "sql");
        var req2 = new CreateRegisteredServerRequest("Name", "sub", "rg", "sql");

        // Act & Assert
        req1.Should().Be(req2);
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var original = new CreateRegisteredServerRequest("Name", "sub", "rg", "sql");

        // Act
        var modified = original with { Name = "NewName" };

        // Assert
        modified.Name.Should().Be("NewName");
        original.Name.Should().Be("Name");
    }
}
