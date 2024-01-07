using Serilog;
using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Server.Services.LogService.Implementations.Seq;

public class SeqLogService : ILogService
{
    public SeqLogService(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Seq(configuration["Logging:Providers:Seq"] ?? string.Empty)
            .CreateLogger();
    }
    
    public async Task LogAsync(LogLevel level, string message)
    {
        Action<string> targetMethod = level switch
        {
            LogLevel.Info => Log.Information,
            LogLevel.Debug => Log.Debug,
            LogLevel.Error => Log.Error,
            LogLevel.Warning => Log.Warning,
            LogLevel.Fatal => Log.Fatal,
            _ => throw new ArgumentException($"Invalid {nameof(LogLevel)} - {level.ToString()}.")
        };
        
        targetMethod.Invoke(message);
        
        await Log.CloseAndFlushAsync();
    }
    
    public async Task LogAsync<T>(LogLevel level, string message, T eventModel)
    {
        Action<string, T> targetMethod = level switch
        {
            LogLevel.Info => Log.Information,
            LogLevel.Debug => Log.Debug,
            LogLevel.Error => Log.Error,
            LogLevel.Warning => Log.Warning,
            LogLevel.Fatal => Log.Fatal,
            _ => throw new ArgumentException($"Invalid {nameof(LogLevel)} - {level.ToString()}.")
        };
        
        targetMethod.Invoke(message, eventModel);
        
        await Log.CloseAndFlushAsync();
    }
    
    public async Task LogAsync<T0, T1>(LogLevel level, string message, T0 eventModel1, T1 eventModel2)
    {
        Action<string, T0, T1> targetMethod = level switch
        {
            LogLevel.Info => Log.Information,
            LogLevel.Debug => Log.Debug,
            LogLevel.Error => Log.Error,
            LogLevel.Warning => Log.Warning,
            LogLevel.Fatal => Log.Fatal,
            _ => throw new ArgumentException($"Invalid {nameof(LogLevel)} - {level.ToString()}.")
        };
        
        targetMethod.Invoke(message, eventModel1, eventModel2);
        
        await Log.CloseAndFlushAsync();
    }
}