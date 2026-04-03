using Microsoft.EntityFrameworkCore;
using SqlDbAnalyze.Abstractions.Exceptions;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;
using SqlDbAnalyze.Repository.Contexts;
using SqlDbAnalyze.Repository.Entities;

namespace SqlDbAnalyze.Repository.Repositories;

public class RegisteredServerRepository(
    IDbContextFactory<AppDbContext> contextFactory)
    : RepositoryBase<AppDbContext>(contextFactory), IRegisteredServerRepository
{
    public virtual async Task<IReadOnlyList<RegisteredServer>> RegisteredServerFindAllAsync(
        CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var entities = await dbContext.RegisteredServers
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public virtual async Task<RegisteredServer> RegisteredServerSingleByIdAsync(
        int id, CancellationToken cancellationToken)
    {
        return await RegisteredServerSingleOrDefaultByIdAsync(id, cancellationToken)
            ?? throw new AzureResourceNotFoundException($"Registered server not found (RegisteredServerId: {id}).");
    }

    public virtual async Task<RegisteredServer?> RegisteredServerSingleOrDefaultByIdAsync(
        int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var entity = await dbContext.RegisteredServers
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.RegisteredServerId == id, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public virtual async Task<RegisteredServer> RegisteredServerCreateAsync(
        RegisteredServer server, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var entity = MapToEntity(server);
        var entry = await dbContext.RegisteredServers.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToDomain(entry.Entity);
    }

    public virtual async Task RegisteredServerDeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var dbContext = await CreateContextAsync(cancellationToken);

        var entity = await dbContext.RegisteredServers
            .FirstOrDefaultAsync(e => e.RegisteredServerId == id, cancellationToken)
            ?? throw new AzureResourceNotFoundException($"Registered server not found (RegisteredServerId: {id}).");

        dbContext.RegisteredServers.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RegisteredServer MapToDomain(RegisteredServerEntity entity)
    {
        return new RegisteredServer(
            entity.RegisteredServerId,
            entity.Name,
            entity.SubscriptionId,
            entity.ResourceGroupName,
            entity.ServerName,
            entity.CreatedAt);
    }

    private static RegisteredServerEntity MapToEntity(RegisteredServer server)
    {
        return new RegisteredServerEntity
        {
            RegisteredServerId = server.RegisteredServerId,
            Name = server.Name,
            SubscriptionId = server.SubscriptionId,
            ResourceGroupName = server.ResourceGroupName,
            ServerName = server.ServerName,
            CreatedAt = server.CreatedAt
        };
    }
}
