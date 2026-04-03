using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Web.Core.Services;
using Xunit;

namespace SqlDbAnalyze.Web.Core.Tests.Extensions;

public class WebCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebCore_ShouldRegisterRegisteredServerService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddWebCore();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IRegisteredServerService)
                 && d.ImplementationType == typeof(RegisteredServerService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddWebCore_ShouldRegisterMetricsCacheService_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddWebCore();

        // Assert
        services.Should().Contain(
            d => d.ServiceType == typeof(IMetricsCacheService)
                 && d.ImplementationType == typeof(MetricsCacheService)
                 && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddWebCore_ShouldReturnServiceCollection_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddWebCore();

        // Assert
        result.Should().BeSameAs(services);
    }
}
