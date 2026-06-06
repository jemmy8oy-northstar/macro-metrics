namespace MacroMetrics.Abstractions.Exceptions;

/// <summary>
/// Thrown by a fetcher service when an external data source returns an unexpected
/// or error response. Callers can catch this to distinguish network/API failures
/// from other application exceptions.
/// </summary>
public sealed class FetcherException : Exception
{
    public FetcherException(string message) : base(message) { }

    public FetcherException(string message, Exception innerException)
        : base(message, innerException) { }
}
