using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Server.Services.LogService;

public interface ILogService
{
    Task LogAsync(Exception exception);
    Task LogAsync(LogLevel level, string message);
    Task LogAsync<T>(LogLevel level, string message, T eventModel);
    Task LogAsync<T0, T1>(LogLevel level, string message, T0 eventModel1, T1 eventModel2);
}