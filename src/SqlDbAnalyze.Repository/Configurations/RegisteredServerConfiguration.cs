using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SqlDbAnalyze.Repository.Entities;

namespace SqlDbAnalyze.Repository.Configurations;

public class RegisteredServerConfiguration : IEntityTypeConfiguration<RegisteredServerEntity>
{
    public void Configure(EntityTypeBuilder<RegisteredServerEntity> builder)
    {
        builder.ToTable("RegisteredServer");
        builder.HasKey(e => e.RegisteredServerId);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.SubscriptionId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ResourceGroupName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ServerName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.SubscriptionId, e.ResourceGroupName, e.ServerName })
            .IsUnique()
            .HasDatabaseName("UQ_RegisteredServer_SubscriptionId_ResourceGroupName_ServerName");

        builder.HasMany(e => e.CachedMetrics)
            .WithOne(e => e.RegisteredServer)
            .HasForeignKey(e => e.RegisteredServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
