using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IRegisteredServerRepository
{
    Task<IReadOnlyList<RegisteredServer>> RegisteredServerFindAllAsync(CancellationToken cancellationToken);

    Task<RegisteredServer> RegisteredServerSingleByIdAsync(int id, CancellationToken cancellationToken);

    Task<RegisteredServer?> RegisteredServerSingleOrDefaultByIdAsync(int id, CancellationToken cancellationToken);

    Task<RegisteredServer> RegisteredServerCreateAsync(RegisteredServer server, CancellationToken cancellationToken);

    Task RegisteredServerDeleteAsync(int id, CancellationToken cancellationToken);
}
