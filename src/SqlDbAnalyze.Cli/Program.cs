using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbAnalyze.Cli;

var rootCommand = RootCommandFactory.Create();

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
