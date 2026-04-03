using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Repository.Entities;

namespace SqlDbAnalyze.Repository.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<RegisteredServerEntity> RegisteredServers => Set<RegisteredServerEntity>();
    public DbSet<CachedDtuMetricEntity> CachedDtuMetrics => Set<CachedDtuMetricEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
