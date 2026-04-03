namespace SqlDbAnalyze.Abstractions.Models;

public record DatabaseInfo(
    string DatabaseName,
    double DataSizeMB,
    int DtuLimit,
    string? ElasticPoolName);
