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

        return services;
    }
}
