using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Implementation.Services;
using Xunit;

namespace SqlDbAnalyze.Implementation.Tests;

public class SqlDbAnalyzeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterDtuAnalysisService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        var descriptor = services.Should().Contain(
            d => d.ServiceType == typeof(IDtuAnalysisService)
                 && d.ImplementationType == typeof(DtuAnalysisService)
                 && d.Lifetime == ServiceLifetime.Scoped)
            .Which;

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterServerAnalysisService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IServerAnalysisService)
                 && d.ImplementationType == typeof(ServerAnalysisService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterAzureMetricsService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IAzureMetricsService)
                 && d.ImplementationType == typeof(AzureMetricsService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldRegisterTimeProvider_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlDbAnalyze();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(TimeProvider)
                 && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSqlDbAnalyze_ShouldReturnServiceCollection_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSqlDbAnalyze();

        // Assert
        result.Should().BeSameAs(services);
    }
}
