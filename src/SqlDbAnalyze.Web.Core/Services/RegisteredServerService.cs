using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Web.Core.Services;

public class RegisteredServerService(
    IRegisteredServerRepository registeredServerRepository,
    IMetricsCacheRepository metricsCacheRepository,
    TimeProvider timeProvider) : IRegisteredServerService
{
    public virtual async Task<IReadOnlyList<RegisteredServer>> GetAllServersAsync(
        CancellationToken cancellationToken)
    {
        return await registeredServerRepository.RegisteredServerFindAllAsync(cancellationToken);
    }

    public virtual async Task<RegisteredServer> CreateServerAsync(
        CreateRegisteredServerRequest request, CancellationToken cancellationToken)
    {
        var server = new RegisteredServer(
            RegisteredServerId: 0,
            Name: request.Name,
            SubscriptionId: request.SubscriptionId,
            ResourceGroupName: request.ResourceGroupName,
            ServerName: request.ServerName,
            CreatedAt: timeProvider.GetUtcNow());

        return await registeredServerRepository.RegisteredServerCreateAsync(server, cancellationToken);
    }

    public virtual async Task DeleteServerAsync(int id, CancellationToken cancellationToken)
    {
        await metricsCacheRepository.MetricsCacheDeleteByServerAsync(id, cancellationToken);
        await registeredServerRepository.RegisteredServerDeleteAsync(id, cancellationToken);
    }
}
