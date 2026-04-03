using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Repository.Contexts;
using SqlDbAnalyze.Repository.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IRegisteredServerRepository, RegisteredServerRepository>();
        services.AddScoped<IMetricsCacheRepository, MetricsCacheRepository>();

        return services;
    }
}
