using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Repository.Contexts;

namespace Microsoft.Extensions.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
