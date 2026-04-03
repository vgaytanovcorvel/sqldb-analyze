using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Web.Core.Services;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Services;

public class RegisteredServerServiceTests
{
    private readonly IRegisteredServerRepository _serverRepo = Substitute.For<IRegisteredServerRepository>();
    private readonly IMetricsCacheRepository _cacheRepo = Substitute.For<IMetricsCacheRepository>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero));

    private RegisteredServerService CreateService() =>
        new(_serverRepo, _cacheRepo, _timeProvider);

    [Fact]
    public async Task GetAllServersAsync_Should_ReturnServers_When_ServersExist()
    {
        // Arrange
        var servers = new List<RegisteredServer>
        {
            new(1, "Server1", "sub", "rg", "sql1", DateTimeOffset.UtcNow)
        };
        _serverRepo.RegisteredServerFindAllAsync(Arg.Any<CancellationToken>()).Returns(servers);
        var service = CreateService();

        // Act
        var result = await service.GetAllServersAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Server1");
    }

    [Fact]
    public async Task CreateServerAsync_Should_SetCreatedAtFromTimeProvider_When_Called()
    {
        // Arrange
        var request = new CreateRegisteredServerRequest("Test", "sub-1", "rg-1", "sql-1");
        _serverRepo.RegisteredServerCreateAsync(Arg.Any<RegisteredServer>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var s = callInfo.Arg<RegisteredServer>();
                return new RegisteredServer(1, s.Name, s.SubscriptionId, s.ResourceGroupName, s.ServerName, s.CreatedAt);
            });
        var service = CreateService();

        // Act
        var result = await service.CreateServerAsync(request, CancellationToken.None);

        // Assert
        result.RegisteredServerId.Should().Be(1);
        result.Name.Should().Be("Test");
        result.CreatedAt.Should().Be(_timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task DeleteServerAsync_Should_DeleteCacheThenServer_When_Called()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.DeleteServerAsync(42, CancellationToken.None);

        // Assert
        await _cacheRepo.Received(1).MetricsCacheDeleteByServerAsync(42, Arg.Any<CancellationToken>());
        await _serverRepo.Received(1).RegisteredServerDeleteAsync(42, Arg.Any<CancellationToken>());
    }
}
