using Client.Application.Gateway;
using EthachatShared.Models.Logging.ExceptionLogging;
using Microsoft.AspNetCore.Components;
using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService.Implementation;

public class LoggingService(NavigationManager navigationManager) : ILoggingService
{
    public async Task LogException(Exception exception)
    {
        _ = new ExceptionLog
        {
            LogLevel = LogLevel.Error,
            Message = exception.Message,
            StackTrace = exception.StackTrace
        };
    }

    public Task<IGateway> GetHubConnectionAsync()
    {
        throw new NotImplementedException();
    }
}