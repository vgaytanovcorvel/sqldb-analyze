using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlDbAnalyze.Repository.Entities;

namespace SqlDbAnalyze.Repository.Configurations;

public class CachedDtuMetricConfiguration : IEntityTypeConfiguration<CachedDtuMetricEntity>
{
    public void Configure(EntityTypeBuilder<CachedDtuMetricEntity> builder)
    {
        builder.ToTable("CachedDtuMetric");
        builder.HasKey(e => e.CachedDtuMetricId);

        builder.Property(e => e.DatabaseName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.DtuPercentage).IsRequired();

        builder.HasIndex(e => new { e.RegisteredServerId, e.DatabaseName, e.Timestamp })
            .IsUnique()
            .HasDatabaseName("UQ_CachedDtuMetric_Server_Database_Timestamp");

        builder.HasIndex(e => e.RegisteredServerId)
            .HasDatabaseName("IX_CachedDtuMetric_RegisteredServerId");
    }
}
