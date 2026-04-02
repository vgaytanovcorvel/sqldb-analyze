using FluentAssertions;
using SqlDbAnalyze.Abstractions.Models;
using Xunit;

namespace SqlDbAnalyze.Abstractions.Tests.Models;

public class ElasticPoolRecommendationTests
{
    [Fact]
    public void Constructor_Should_SetAllProperties_When_ValidArgumentsProvided()
    {
        // Arrange
        var summaries = new List<DatabaseDtuSummary>
        {
            new("Db1", 30.0, 80.0, 100),
            new("Db2", 50.0, 95.0, 200)
        };

        // Act
        var recommendation = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);

        // Assert
        recommendation.RecommendedTier.Should().Be("Standard");
        recommendation.RecommendedDtu.Should().Be(200);
        recommendation.EstimatedTotalDtuUsage.Should().Be(150.5);
        recommendation.DatabaseSummaries.Should().HaveCount(2);
    }

    [Fact]
    public void DatabaseSummaries_Should_ContainExpectedItems_When_ListProvided()
    {
        // Arrange
        var summary1 = new DatabaseDtuSummary("Db1", 30.0, 80.0, 100);
        var summary2 = new DatabaseDtuSummary("Db2", 50.0, 95.0, 200);
        var summaries = new List<DatabaseDtuSummary> { summary1, summary2 };

        // Act
        var recommendation = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);

        // Assert
        recommendation.DatabaseSummaries.Should().ContainInOrder(summary1, summary2);
        recommendation.DatabaseSummaries[0].DatabaseName.Should().Be("Db1");
        recommendation.DatabaseSummaries[1].DatabaseName.Should().Be("Db2");
    }

    [Fact]
    public void DatabaseSummaries_Should_BeEmpty_When_EmptyListProvided()
    {
        // Arrange
        var summaries = new List<DatabaseDtuSummary>();

        // Act
        var recommendation = new ElasticPoolRecommendation("Basic", 50, 0.0, summaries);

        // Assert
        recommendation.DatabaseSummaries.Should().BeEmpty();
    }

    [Fact]
    public void Equality_Should_BeTrue_When_RecordsHaveSameValuesAndSameListReference()
    {
        // Arrange
        IReadOnlyList<DatabaseDtuSummary> summaries = new List<DatabaseDtuSummary>
        {
            new("Db1", 30.0, 80.0, 100)
        };
        var rec1 = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);
        var rec2 = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);

        // Act
        var areEqual = rec1 == rec2;

        // Assert
        rec1.Should().Be(rec2);
        areEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_DifferentListInstancesWithSameData()
    {
        // Arrange
        var summaries1 = new List<DatabaseDtuSummary> { new("Db1", 30.0, 80.0, 100) };
        var summaries2 = new List<DatabaseDtuSummary> { new("Db1", 30.0, 80.0, 100) };
        var rec1 = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries1);
        var rec2 = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries2);

        // Act
        var areEqual = rec1 == rec2;

        // Assert
        areEqual.Should().BeFalse();
        rec1.Should().NotBe(rec2);
    }

    [Fact]
    public void Equality_Should_BeFalse_When_RecommendedTierDiffers()
    {
        // Arrange
        IReadOnlyList<DatabaseDtuSummary> summaries = new List<DatabaseDtuSummary>();
        var rec1 = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);
        var rec2 = new ElasticPoolRecommendation("Premium", 200, 150.5, summaries);

        // Act
        var areNotEqual = rec1 != rec2;

        // Assert
        rec1.Should().NotBe(rec2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_BeFalse_When_RecommendedDtuDiffers()
    {
        // Arrange
        IReadOnlyList<DatabaseDtuSummary> summaries = new List<DatabaseDtuSummary>();
        var rec1 = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);
        var rec2 = new ElasticPoolRecommendation("Standard", 400, 150.5, summaries);

        // Act
        var areNotEqual = rec1 != rec2;

        // Assert
        rec1.Should().NotBe(rec2);
        areNotEqual.Should().BeTrue();
    }

    [Fact]
    public void WithExpression_Should_CreateModifiedCopy_When_PropertyChanged()
    {
        // Arrange
        var summaries = new List<DatabaseDtuSummary>
        {
            new("Db1", 30.0, 80.0, 100)
        };
        var original = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);

        // Act
        var modified = original with { RecommendedTier = "Premium", RecommendedDtu = 500 };

        // Assert
        modified.RecommendedTier.Should().Be("Premium");
        modified.RecommendedDtu.Should().Be(500);
        modified.EstimatedTotalDtuUsage.Should().Be(original.EstimatedTotalDtuUsage);
        modified.DatabaseSummaries.Should().BeSameAs(original.DatabaseSummaries);
    }

    [Fact]
    public void WithExpression_Should_PreserveOriginal_When_CopyCreated()
    {
        // Arrange
        var summaries = new List<DatabaseDtuSummary>();
        var original = new ElasticPoolRecommendation("Standard", 200, 150.5, summaries);

        // Act
        _ = original with { RecommendedDtu = 999 };

        // Assert
        original.RecommendedDtu.Should().Be(200);
    }

    [Fact]
    public void WithExpression_Should_ReplaceDatabaseSummaries_When_NewListProvided()
    {
        // Arrange
        var originalSummaries = new List<DatabaseDtuSummary> { new("Db1", 30.0, 80.0, 100) };
        var original = new ElasticPoolRecommendation("Standard", 200, 150.5, originalSummaries);
        var newSummaries = new List<DatabaseDtuSummary>
        {
            new("Db2", 60.0, 90.0, 300),
            new("Db3", 40.0, 70.0, 100)
        };

        // Act
        var modified = original with { DatabaseSummaries = newSummaries };

        // Assert
        modified.DatabaseSummaries.Should().HaveCount(2);
        modified.DatabaseSummaries[0].DatabaseName.Should().Be("Db2");
        original.DatabaseSummaries.Should().HaveCount(1);
    }

    [Fact]
    public void ToString_Should_ContainPropertyValues_When_Called()
    {
        // Arrange
        var summaries = new List<DatabaseDtuSummary>();
        var recommendation = new ElasticPoolRecommendation("Premium", 400, 275.0, summaries);

        // Act
        var result = recommendation.ToString();

        // Assert
        result.Should().Contain("Premium");
        result.Should().Contain("400");
        result.Should().Contain("275");
    }
}
