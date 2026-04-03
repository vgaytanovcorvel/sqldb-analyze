using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Abstractions.Exceptions;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Repository.Contexts;
using SqlDbAnalyze.Repository.Repositories;
using Xunit;

namespace SqlDbAnalyze.Repository.Tests.Repositories;

public class RegisteredServerRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RegisteredServerRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();

        _factory = new TestDbContextFactory(_options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task RegisteredServerFindAllAsync_Should_ReturnEmpty_When_NoServersExist()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);

        // Act
        var result = await repo.RegisteredServerFindAllAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisteredServerCreateAsync_Should_ReturnServerWithId_When_ValidServerProvided()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);
        var server = new RegisteredServer(0, "Test", "sub-1", "rg-1", "sql-1", DateTimeOffset.UtcNow);

        // Act
        var result = await repo.RegisteredServerCreateAsync(server, CancellationToken.None);

        // Assert
        result.RegisteredServerId.Should().BeGreaterThan(0);
        result.Name.Should().Be("Test");
        result.SubscriptionId.Should().Be("sub-1");
    }

    [Fact]
    public async Task RegisteredServerFindAllAsync_Should_ReturnAllServers_When_ServersExist()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);
        await repo.RegisteredServerCreateAsync(
            new RegisteredServer(0, "A", "sub", "rg", "sql-a", DateTimeOffset.UtcNow), CancellationToken.None);
        await repo.RegisteredServerCreateAsync(
            new RegisteredServer(0, "B", "sub", "rg", "sql-b", DateTimeOffset.UtcNow), CancellationToken.None);

        // Act
        var result = await repo.RegisteredServerFindAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("A");
        result[1].Name.Should().Be("B");
    }

    [Fact]
    public async Task RegisteredServerSingleByIdAsync_Should_ReturnServer_When_ServerExists()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);
        var created = await repo.RegisteredServerCreateAsync(
            new RegisteredServer(0, "Test", "sub", "rg", "sql", DateTimeOffset.UtcNow), CancellationToken.None);

        // Act
        var result = await repo.RegisteredServerSingleByIdAsync(created.RegisteredServerId, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task RegisteredServerSingleByIdAsync_Should_Throw_When_ServerDoesNotExist()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);

        // Act
        var act = () => repo.RegisteredServerSingleByIdAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzureResourceNotFoundException>();
    }

    [Fact]
    public async Task RegisteredServerSingleOrDefaultByIdAsync_Should_ReturnNull_When_ServerDoesNotExist()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);

        // Act
        var result = await repo.RegisteredServerSingleOrDefaultByIdAsync(999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisteredServerDeleteAsync_Should_RemoveServer_When_ServerExists()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);
        var created = await repo.RegisteredServerCreateAsync(
            new RegisteredServer(0, "ToDelete", "sub", "rg", "sql", DateTimeOffset.UtcNow), CancellationToken.None);

        // Act
        await repo.RegisteredServerDeleteAsync(created.RegisteredServerId, CancellationToken.None);

        // Assert
        var result = await repo.RegisteredServerSingleOrDefaultByIdAsync(created.RegisteredServerId, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisteredServerDeleteAsync_Should_Throw_When_ServerDoesNotExist()
    {
        // Arrange
        var repo = new RegisteredServerRepository(_factory);

        // Act
        var act = () => repo.RegisteredServerDeleteAsync(999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AzureResourceNotFoundException>();
    }

    private class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
