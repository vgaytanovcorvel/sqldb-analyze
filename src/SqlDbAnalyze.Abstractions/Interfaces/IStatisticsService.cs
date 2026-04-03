namespace SqlDbAnalyze.Abstractions.Interfaces;

public interface IStatisticsService
{
    double Mean(IReadOnlyList<double> values);

    double Percentile(IReadOnlyList<double> values, double p);

    double PercentilePreSorted(double[] sortedValues, double p);

    double PearsonCorrelation(IReadOnlyList<double> x, IReadOnlyList<double> y);

    IReadOnlyList<double> SumSeries(IReadOnlyList<IReadOnlyList<double>> series);

    double OverloadFraction(IReadOnlyList<double> values, double threshold);
}
