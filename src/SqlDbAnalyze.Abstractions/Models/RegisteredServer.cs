namespace SqlDbAnalyze.Abstractions.Models;

public record RegisteredServer(
    int RegisteredServerId,
    string Name,
    string SubscriptionId,
    string ResourceGroupName,
    string ServerName,
    DateTimeOffset CreatedAt);
