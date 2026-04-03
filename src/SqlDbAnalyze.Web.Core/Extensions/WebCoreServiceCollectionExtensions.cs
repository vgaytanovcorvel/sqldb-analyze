using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Web.Core.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebCoreServiceCollectionExtensions
{
    public static IServiceCollection AddWebCore(this IServiceCollection services)
    {
        services.AddScoped<IRegisteredServerService, RegisteredServerService>();
        services.AddScoped<IMetricsCacheService, MetricsCacheService>();

        return services;
    }
}
