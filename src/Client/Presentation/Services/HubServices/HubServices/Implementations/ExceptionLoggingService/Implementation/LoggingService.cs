using Client.Application.Gateway;
using Client.Infrastructure.Gateway;
using EthachatShared.Constants;
using EthachatShared.Models.Logging.ExceptionLogging;
using Microsoft.AspNetCore.Components;
using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService.Implementation;

public class LoggingService(NavigationManager navigationManager) : ILoggingService
{
    private IGateway? _gateway;

    private async Task<IGateway> ConfigureGateway()
    {
        var gateway = new SignalRGateway();
        await gateway.ConfigureAsync(navigationManager.ToAbsoluteUri(HubAddress.ExceptionLogging));
        return gateway;
    }

    public async Task LogException(Exception exception)
    {
        var exceptionLog = new ExceptionLog
        {
            LogLevel = LogLevel.Error,
            Message = exception.Message,
            StackTrace = exception.StackTrace
        };

        var gateway = await GetHubConnectionAsync();
        await gateway.SendAsync("Log", exceptionLog);
    }
    
    public async Task<IGateway> GetHubConnectionAsync()
    {
        return _gateway ??= await ConfigureGateway(); 
    }
}