using Microsoft.EntityFrameworkCore;

namespace SqlDbAnalyze.Repository;

public abstract class RepositoryBase<TContext>(
    IDbContextFactory<TContext> contextFactory) where TContext : DbContext
{
    protected virtual async Task<TContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        return await contextFactory.CreateDbContextAsync(cancellationToken);
    }
}
