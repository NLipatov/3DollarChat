using Ethachat.Server.Services.LogService;
using EthachatShared.Models.Logging.ExceptionLogging;
using Microsoft.AspNetCore.SignalR;
using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Server.Hubs;

public class LoggingHub(ILogService logService) : Hub
{
    public async Task Log(ExceptionLog exceptionLog)
    {
        await logService.LogAsync(LogLevel.Error, "{@log}", exceptionLog);
    }
}