using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class SqlDbAnalyzeServiceCollectionExtensionsAdditionalTests
{
    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterStatisticsService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IStatisticsService)
                 && d.ImplementationType == typeof(StatisticsService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterPoolabilityService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IPoolabilityService)
                 && d.ImplementationType == typeof(PoolabilityService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterPlacementScorer_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IPlacementScorer)
                 && d.ImplementationType == typeof(PlacementScorer)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterPoolBuilder_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IPoolBuilder)
                 && d.ImplementationType == typeof(PoolBuilder)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterLocalSearchOptimizer_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(ILocalSearchOptimizer)
                 && d.ImplementationType == typeof(LocalSearchOptimizer)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterTimeSeriesCsvService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(ITimeSeriesCsvService)
                 && d.ImplementationType == typeof(TimeSeriesCsvService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterCaptureService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(ICaptureService)
                 && d.ImplementationType == typeof(CaptureService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }
}
