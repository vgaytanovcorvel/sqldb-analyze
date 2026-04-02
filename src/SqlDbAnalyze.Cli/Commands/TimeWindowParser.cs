using System.CommandLine;
using SqlDbAnalyze.Abstractions.Models;

namespace SqlDbAnalyze.Cli.Commands;

public static class TimeWindowParser
{
    public static AnalysisTimeWindow? Parse(
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
