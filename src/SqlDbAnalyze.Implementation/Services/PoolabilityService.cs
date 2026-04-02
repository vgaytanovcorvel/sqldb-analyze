using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Implementation.Services;

public class PoolabilityService(IStatisticsService statisticsService) : IPoolabilityService
{
    public virtual IReadOnlyList<DatabaseProfile> BuildProfiles(DtuTimeSeries timeSeries)
    {
        return timeSeries.DatabaseValues
            .Select(kv => BuildSingleProfile(kv.Key, kv.Value))
            .OrderBy(p => p.DatabaseName)
            .ToList();
    }

    public virtual PoolabilityMetrics ComputePairwise(
        DatabaseProfile a,
        DatabaseProfile b,
        double peakThreshold)
    {
        var fullCorr = statisticsService.PearsonCorrelation(a.DtuValues, b.DtuValues);
        var (peakCorr, peakOverlap) = ComputePeakMetrics(a, b, peakThreshold);
        var badScore = ComputeBadTogetherScore(fullCorr, peakCorr, peakOverlap);

        return new PoolabilityMetrics(
            a.DatabaseName, b.DatabaseName,
            fullCorr, peakCorr, peakOverlap, badScore);
    }

    private DatabaseProfile BuildSingleProfile(string name, IReadOnlyList<double> values)
    {
        return new DatabaseProfile(
            name,
            values,
            statisticsService.Mean(values),
            statisticsService.Percentile(values, 0.95),
            statisticsService.Percentile(values, 0.99),
            values.Count > 0 ? values.Max() : 0);
    }

    private (double PeakCorr, double PeakOverlap) ComputePeakMetrics(
        DatabaseProfile a,
        DatabaseProfile b,
        double peakThreshold)
    {
        var aThreshold = statisticsService.Percentile(a.DtuValues, peakThreshold);
        var bThreshold = statisticsService.Percentile(b.DtuValues, peakThreshold);

        var aPeakValues = new List<double>();
        var bPeakValues = new List<double>();
        int overlapCount = 0, eitherPeakCount = 0;

        CollectPeakIntervals(a.DtuValues, b.DtuValues, aThreshold, bThreshold,
            aPeakValues, bPeakValues, ref overlapCount, ref eitherPeakCount);

        var peakCorr = aPeakValues.Count >= 2
            ? statisticsService.PearsonCorrelation(aPeakValues, bPeakValues)
            : 0;

        var peakOverlap = eitherPeakCount == 0 ? 0 : (double)overlapCount / eitherPeakCount;

        return (peakCorr, peakOverlap);
    }

    private static void CollectPeakIntervals(
        IReadOnlyList<double> aValues,
        IReadOnlyList<double> bValues,
        double aThreshold,
        double bThreshold,
        List<double> aPeakOut,
        List<double> bPeakOut,
        ref int overlapCount,
        ref int eitherPeakCount)
    {
        for (var i = 0; i < aValues.Count; i++)
        {
            var aPeak = aValues[i] >= aThreshold;
            var bPeak = bValues[i] >= bThreshold;

            if (!aPeak && !bPeak) continue;

            eitherPeakCount++;
            aPeakOut.Add(aValues[i]);
            bPeakOut.Add(bValues[i]);

            if (aPeak && bPeak) overlapCount++;
        }
    }

    private static double ComputeBadTogetherScore(
        double fullCorr,
        double peakCorr,
        double peakOverlap)
    {
        var fullCorrBad = Math.Max(0, fullCorr);
        var peakCorrBad = Math.Max(0, peakCorr);

        return 0.20 * fullCorrBad + 0.40 * peakCorrBad + 0.40 * peakOverlap;
    }
}
