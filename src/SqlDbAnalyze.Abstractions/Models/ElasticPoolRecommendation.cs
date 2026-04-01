namespace SqlDbAnalyze.Abstractions.Models;

public record ElasticPoolRecommendation(
    string RecommendedTier,
    int RecommendedDtu,
    double EstimatedTotalDtuUsage,
    IReadOnlyList<DatabaseDtuSummary> DatabaseSummaries);
