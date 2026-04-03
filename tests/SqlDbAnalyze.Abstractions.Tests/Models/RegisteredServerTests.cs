using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class RegisteredServerTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var id = 1;
        var name = "Prod Server";
        var subscriptionId = "sub-123";
        var resourceGroupName = "rg-prod";
        var serverName = "sql-prod";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var server = new RegisteredServer(id, name, subscriptionId, resourceGroupName, serverName, createdAt);

        // Assert
        server.RegisteredServerId.Should().Be(id);
        server.Name.Should().Be(name);
        server.SubscriptionId.Should().Be(subscriptionId);
        server.ResourceGroupName.Should().Be(resourceGroupName);
        server.ServerName.Should().Be(serverName);
        server.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var server1 = new RegisteredServer(1, "Server", "sub", "rg", "sql", createdAt);
        var server2 = new RegisteredServer(1, "Server", "sub", "rg", "sql", createdAt);

        // Act & Assert
        server1.Should().Be(server2);
        (server1 == server2).Should().BeTrue();
        server1.GetHashCode().Should().Be(server2.GetHashCode());
    }

    [Fact]
    public void Equality_Should_BeFalse_When_NameDiffers()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var server1 = new RegisteredServer(1, "Server1", "sub", "rg", "sql", createdAt);
        var server2 = new RegisteredServer(1, "Server2", "sub", "rg", "sql", createdAt);

        // Act & Assert
        server1.Should().NotBe(server2);
        (server1 != server2).Should().BeTrue();
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var original = new RegisteredServer(1, "Original", "sub", "rg", "sql", DateTimeOffset.UtcNow);

        // Act
        var modified = original with { Name = "Modified" };

        // Assert
        modified.Name.Should().Be("Modified");
        modified.RegisteredServerId.Should().Be(original.RegisteredServerId);
        original.Name.Should().Be("Original");
    }

    [Fact]
    public void ToString_Should_ContainPropertyValues_When_Called()
    {
        // Arrange
        var server = new RegisteredServer(1, "TestServer", "sub-123", "rg-test", "sql-test", DateTimeOffset.MinValue);

        // Act
        var result = server.ToString();

        // Assert
        result.Should().Contain("TestServer");
        result.Should().Contain("sub-123");
        result.Should().Contain("sql-test");
    }
}
