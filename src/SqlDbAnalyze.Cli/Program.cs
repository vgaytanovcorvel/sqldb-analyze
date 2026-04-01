using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbAnalyze.Cli.Commands;

var rootCommand = new RootCommand("sqldb-analyze - Analyze SQL Server DTU usage and recommend elastic pool sizing");

var verboseOption = new Option<bool>(
    ["--verbose", "-v"],
    "Increase output detail");

rootCommand.AddGlobalOption(verboseOption);
rootCommand.AddCommand(new AnalyzeCommand());

return await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseHost(Host.CreateDefaultBuilder, host =>
    {
        host.ConfigureServices((ctx, services) =>
        {
            services.AddSqlDbAnalyze();
        });
    })
    .Build()
    .InvokeAsync(args);
