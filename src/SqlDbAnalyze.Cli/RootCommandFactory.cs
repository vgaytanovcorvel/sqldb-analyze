using System.CommandLine;
using SqlDbAnalyze.Cli.Commands;

namespace SqlDbAnalyze.Cli;

public static class RootCommandFactory
{
    public static RootCommand Create()
    {
        var rootCommand = new RootCommand("sqldb-analyze - Analyze SQL Server DTU usage and recommend elastic pool sizing");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            "Increase output detail");

        rootCommand.AddGlobalOption(verboseOption);
        rootCommand.AddCommand(new AnalyzeCommand());

        return rootCommand;
    }
}
