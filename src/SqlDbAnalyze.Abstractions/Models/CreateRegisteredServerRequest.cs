namespace SqlDbAnalyze.Abstractions.Models;

public record CreateRegisteredServerRequest(
    string Name,
    string SubscriptionId,
    string ResourceGroupName,
    string ServerName);
