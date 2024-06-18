#nullable disable
using EthachatShared.Models.Logging.ExceptionLogging;

namespace EthachatShared.Models.Logging;

public record Log
{
    public string Message { get; set; }
    public LogLevel LogLevel { get; set; }
}