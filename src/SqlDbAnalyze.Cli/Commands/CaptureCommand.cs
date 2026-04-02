using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbAnalyze.Abstractions.Interfaces;

namespace SqlDbAnalyze.Cli.Commands;

public class CaptureCommand : Command
{
    private readonly Option<string> _subscriptionOption = new(
        ["--subscription", "-s"],
        "Azure subscription ID")
    { IsRequired = true };

    private readonly Option<string> _resourceGroupOption = new(
        ["--resource-group", "-g"],
        "Azure resource group name")
    { IsRequired = true };

    private readonly Argument<string> _serverArgument = new(
        "server-name",
        "Name of the Azure SQL Server to capture metrics from");

    private readonly Option<int> _hoursOption = new(
        ["--hours"],
        getDefaultValue: () => 24,
        "Number of hours of metrics to capture");

    private readonly Option<string> _outputOption = new(
        ["--output", "-o"],
        getDefaultValue: () => "dtu-metrics.csv",
        "Output CSV file path");

    private readonly Option<string?> _windowStartOption = new(
        ["--window-start"],
        "Start time of daily analysis window (e.g., 09:00)");

    private readonly Option<string?> _windowEndOption = new(
        ["--window-end"],
        "End time of daily analysis window (e.g., 17:00)");

    private readonly Option<string> _windowTimezoneOption = new(
        ["--window-timezone"],
        getDefaultValue: () => "Eastern Standard Time",
        "Time zone for the analysis window");

    public CaptureCommand() : base("capture", "Capture historical DTU metrics and export as CSV time series")
    {
        AddOption(_subscriptionOption);
        AddOption(_resourceGroupOption);
        AddArgument(_serverArgument);
        AddOption(_hoursOption);
        AddOption(_outputOption);
        AddOption(_windowStartOption);
        AddOption(_windowEndOption);
        AddOption(_windowTimezoneOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var host = context.BindingContext.GetRequiredService<IHost>();
            var captureService = host.Services.GetRequiredService<ICaptureService>();
            var csvService = host.Services.GetRequiredService<ITimeSeriesCsvService>();

            var subscription = context.ParseResult.GetValueForOption(_subscriptionOption)!;
            var resourceGroup = context.ParseResult.GetValueForOption(_resourceGroupOption)!;
            var serverName = context.ParseResult.GetValueForArgument(_serverArgument);
            var hours = context.ParseResult.GetValueForOption(_hoursOption);
            var output = context.ParseResult.GetValueForOption(_outputOption)!;
            var windowStart = context.ParseResult.GetValueForOption(_windowStartOption);
            var windowEnd = context.ParseResult.GetValueForOption(_windowEndOption);
            var windowTimezone = context.ParseResult.GetValueForOption(_windowTimezoneOption)!;
            var ct = context.GetCancellationToken();
            var console = context.Console;

            var timeWindow = TimeWindowParser.Parse(windowStart, windowEnd, windowTimezone, console);
            if (timeWindow is null && (windowStart is not null || windowEnd is not null))
            {
                context.ExitCode = 2;
                return;
            }

            console.WriteLine($"Capturing DTU metrics for server '{serverName}' over the last {hours} hours...");

            var timeSeries = await captureService.CaptureMetricsAsync(
                subscription, resourceGroup, serverName,
                TimeSpan.FromHours(hours), timeWindow, ct);

            await csvService.WriteAsync(timeSeries, output, ct);

            console.WriteLine($"Captured {timeSeries.DatabaseValues.Count} databases, " +
                              $"{timeSeries.Timestamps.Count} data points each.");
            console.WriteLine($"Output written to: {output}");
        });
    }
}
