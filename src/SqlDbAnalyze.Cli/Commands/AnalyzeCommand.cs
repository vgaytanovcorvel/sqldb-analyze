using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbAnalyze.Abstractions.Interfaces;

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

    public AnalyzeCommand() : base("analyze", "Analyze DTU usage for all databases on a SQL Server")
    {
        AddOption(_subscriptionOption);
        AddOption(_resourceGroupOption);
        AddArgument(_serverArgument);
        AddOption(_hoursOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var host = context.BindingContext.GetRequiredService<IHost>();
            var analysisService = host.Services.GetRequiredService<IServerAnalysisService>();

            var subscription = context.ParseResult.GetValueForOption(_subscriptionOption)!;
            var resourceGroup = context.ParseResult.GetValueForOption(_resourceGroupOption)!;
            var serverName = context.ParseResult.GetValueForArgument(_serverArgument);
            var hours = context.ParseResult.GetValueForOption(_hoursOption);
            var ct = context.GetCancellationToken();

            var console = context.Console;
            console.WriteLine($"Analyzing DTU usage for server '{serverName}' over the last {hours} hours...");
            console.WriteLine("");

            var recommendation = await analysisService.AnalyzeServerAsync(
                subscription, resourceGroup, serverName,
                TimeSpan.FromHours(hours), ct);

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
}
