namespace SqlDbAnalyze.Abstractions.Exceptions;

public class AzureResourceNotFoundException : Exception
{
    public AzureResourceNotFoundException(string message) : base(message) { }
}
