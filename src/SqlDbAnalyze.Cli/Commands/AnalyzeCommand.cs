using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbAnalyze.Abstractions.Interfaces;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Cli.Commands;

public class AnalyzeCommand : Command
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
        "Name of the Azure SQL Server to analyze");

    private readonly Option<int> _hoursOption = new(
        ["--hours"],
        getDefaultValue: () => 24,
        "Number of hours of metrics to analyze");

    private readonly Option<string?> _windowStartOption = new(
        ["--window-start"],
        "Start time of daily analysis window (e.g., 09:00)");

    private readonly Option<string?> _windowEndOption = new(
        ["--window-end"],
        "End time of daily analysis window (e.g., 17:00)");

    private readonly Option<string> _windowTimezoneOption = new(
        ["--window-timezone"],
        getDefaultValue: () => "Eastern Standard Time",
        "Time zone for the analysis window (e.g., 'Eastern Standard Time', 'Pacific Standard Time')");

    public AnalyzeCommand() : base("analyze", "Analyze DTU usage for all databases on a SQL Server")
    {
        AddOption(_subscriptionOption);
        AddOption(_resourceGroupOption);
        AddArgument(_serverArgument);
        AddOption(_hoursOption);
        AddOption(_windowStartOption);
        AddOption(_windowEndOption);
        AddOption(_windowTimezoneOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var host = context.BindingContext.GetRequiredService<IHost>();
            var analysisService = host.Services.GetRequiredService<IServerAnalysisService>();

            var subscription = context.ParseResult.GetValueForOption(_subscriptionOption)!;
            var resourceGroup = context.ParseResult.GetValueForOption(_resourceGroupOption)!;
            var serverName = context.ParseResult.GetValueForArgument(_serverArgument);
            var hours = context.ParseResult.GetValueForOption(_hoursOption);
            var windowStart = context.ParseResult.GetValueForOption(_windowStartOption);
            var windowEnd = context.ParseResult.GetValueForOption(_windowEndOption);
            var windowTimezone = context.ParseResult.GetValueForOption(_windowTimezoneOption)!;
            var ct = context.GetCancellationToken();
            var console = context.Console;

            var timeWindow = ParseTimeWindow(windowStart, windowEnd, windowTimezone, console);
            if (timeWindow is null && (windowStart is not null || windowEnd is not null))
            {
                context.ExitCode = 2;
                return;
            }

            console.WriteLine($"Analyzing DTU usage for server '{serverName}' over the last {hours} hours...");
            if (timeWindow is not null)
            {
                console.WriteLine(
                    $"  Time window: {timeWindow.StartTime} - {timeWindow.EndTime} ({timeWindow.TimeZoneId})");
            }

            console.WriteLine("");

            var recommendation = await analysisService.AnalyzeServerAsync(
                subscription, resourceGroup, serverName,
                TimeSpan.FromHours(hours), timeWindow, ct);

            console.WriteLine("Database DTU Summary:");
            console.WriteLine(new string('-', 70));
            console.WriteLine(
                $"{"Database",-30} {"Avg DTU%",10} {"Peak DTU%",10} {"DTU Limit",10}");
            console.WriteLine(new string('-', 70));

            foreach (var db in recommendation.DatabaseSummaries)
            {
                console.WriteLine(
                    $"{db.DatabaseName,-30} {db.AverageDtuPercent,9:F1}% {db.PeakDtuPercent,9:F1}% {db.CurrentDtuLimit,10}");
            }

            console.WriteLine("");
            console.WriteLine("Elastic Pool Recommendation:");
            console.WriteLine($"  Tier:           {recommendation.RecommendedTier}");
            console.WriteLine($"  Pool DTUs:      {recommendation.RecommendedDtu}");
            console.WriteLine($"  Est. Total DTU: {recommendation.EstimatedTotalDtuUsage:F1}");
        });
    }

    private static AnalysisTimeWindow? ParseTimeWindow(
        string? windowStart,
        string? windowEnd,
        string windowTimezone,
        IConsole console)
    {
        if (windowStart is null && windowEnd is null)
            return null;

        if (windowStart is null || windowEnd is null)
        {
            console.Error.Write(
                "error: both --window-start and --window-end must be provided together.\n");
            return null;
        }

        if (!TimeOnly.TryParse(windowStart, out var startTime))
        {
            console.Error.Write(
                $"error: invalid --window-start value '{windowStart}'. Use HH:mm format (e.g., 09:00).\n");
            return null;
        }

        if (!TimeOnly.TryParse(windowEnd, out var endTime))
        {
            console.Error.Write(
                $"error: invalid --window-end value '{windowEnd}'. Use HH:mm format (e.g., 17:00).\n");
            return null;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(windowTimezone);
        }
        catch (TimeZoneNotFoundException)
        {
            console.Error.Write(
                $"error: unknown time zone '{windowTimezone}'.\n");
            return null;
        }

        return new AnalysisTimeWindow(startTime, endTime, windowTimezone);
    }
}
