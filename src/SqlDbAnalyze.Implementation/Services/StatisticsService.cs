using SqlDbAnalyze.Abstractions.Interfaces;

namespace SqlDbAnalyze.Implementation.Services;

public class StatisticsService : IStatisticsService
{
    public virtual double Mean(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;

        double sum = 0;
        for (var i = 0; i < values.Count; i++)
            sum += values[i];

        return sum / values.Count;
    }

    public virtual double Percentile(IReadOnlyList<double> values, double p)
    {
        if (values.Count == 0) return 0;

        var sorted = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
            sorted[i] = values[i];
        Array.Sort(sorted);

        return PercentilePreSorted(sorted, p);
    }

    public virtual double PercentilePreSorted(double[] sortedValues, double p)
    {
        if (sortedValues.Length == 0) return 0;
        if (p <= 0) return sortedValues[0];
        if (p >= 1) return sortedValues[^1];

        var index = (sortedValues.Length - 1) * p;
        var lo = (int)Math.Floor(index);
        var hi = (int)Math.Ceiling(index);

        return lo == hi
            ? sortedValues[lo]
            : sortedValues[lo] + (sortedValues[hi] - sortedValues[lo]) * (index - lo);
    }

    public virtual double PearsonCorrelation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count != y.Count || x.Count < 2) return 0;

        var meanX = Mean(x);
        var meanY = Mean(y);

        return ComputeCorrelation(x, y, meanX, meanY);
    }

    public virtual IReadOnlyList<double> SumSeries(IReadOnlyList<IReadOnlyList<double>> series)
    {
        if (series.Count == 0) return [];

        var length = series[0].Count;
        var result = new double[length];

        foreach (var s in series)
            for (var i = 0; i < length; i++)
                result[i] += s[i];

        return result;
    }

    public virtual double OverloadFraction(IReadOnlyList<double> values, double threshold)
    {
        if (values.Count == 0 || threshold <= 0) return 0;

        var count = 0;
        for (var i = 0; i < values.Count; i++)
            if (values[i] > threshold)
                count++;

        return (double)count / values.Count;
    }

    public virtual double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;

        var mean = Mean(values);
        double sumSqDev = 0;
        for (var i = 0; i < values.Count; i++)
        {
            var dev = values[i] - mean;
            sumSqDev += dev * dev;
        }

        return Math.Sqrt(sumSqDev / values.Count);
    }

    private static double ComputeCorrelation(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double meanX,
        double meanY)
    {
        double cov = 0, varX = 0, varY = 0;
        for (var i = 0; i < x.Count; i++)
        {
            var dx = x[i] - meanX;
            var dy = y[i] - meanY;
            cov += dx * dy;
            varX += dx * dx;
            varY += dy * dy;
        }

        return varX <= 1e-12 || varY <= 1e-12
            ? 0
            : cov / Math.Sqrt(varX * varY);
    }
}
