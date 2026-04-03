using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IRegisteredServerService
{
    Task<IReadOnlyList<RegisteredServer>> GetAllServersAsync(CancellationToken cancellationToken);

    Task<RegisteredServer> CreateServerAsync(CreateRegisteredServerRequest request, CancellationToken cancellationToken);

    Task DeleteServerAsync(int id, CancellationToken cancellationToken);
}
