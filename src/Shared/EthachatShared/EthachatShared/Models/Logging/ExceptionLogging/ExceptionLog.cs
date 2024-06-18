namespace EthachatShared.Models.Logging.ExceptionLogging;

public record ExceptionLog : Log
{
    public string? StackTrace { get; set; }
}