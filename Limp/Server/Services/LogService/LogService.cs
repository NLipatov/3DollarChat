namespace Ethachat.Server.Services.LogService;

public interface LogService
{
    Task LogAsync(LogLevel level, string message);
    Task LogAsync<T>(LogLevel level, string message, T eventModel);
    Task LogAsync<T0, T1>(LogLevel level, string message, T0 eventModel1, T1 eventModel2);
}