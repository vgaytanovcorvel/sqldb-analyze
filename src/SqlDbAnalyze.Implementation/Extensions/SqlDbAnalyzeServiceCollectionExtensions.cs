using Azure.Identity;
using Azure.Monitor.Query;
using Azure.ResourceManager;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Implementation.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class SqlDbAnalyzeServiceCollectionExtensions
{
    public static IServiceCollection AddSqlDbAnalyze(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new ArmClient(new DefaultAzureCredential()));
        services.AddSingleton(_ => new MetricsQueryClient(new DefaultAzureCredential()));
        services.AddScoped<IAzureMetricsService, AzureMetricsService>();
        services.AddScoped<IDtuAnalysisService, DtuAnalysisService>();
        services.AddScoped<IServerAnalysisService, ServerAnalysisService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IPoolabilityService, PoolabilityService>();
        services.AddScoped<IPlacementScorer, PlacementScorer>();
        services.AddScoped<IFillerPoolBuilder, FillerPoolBuilder>();
        services.AddScoped<IPoolBuilder, PoolBuilder>();
        services.AddScoped<ILocalSearchOptimizer, LocalSearchOptimizer>();
        services.AddScoped<ITimeSeriesCsvService, TimeSeriesCsvService>();
        services.AddScoped<ICaptureService, CaptureService>();

        return services;
    }
}
