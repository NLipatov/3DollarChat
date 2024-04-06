using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using EthachatShared.Constants;
using EthachatShared.Models.Logging.ExceptionLogging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using LogLevel = EthachatShared.Models.Logging.ExceptionLogging.LogLevel;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService.Implementation;

public class LoggingService : ILoggingService
{
    private readonly ICallbackExecutor _callbackExecutor;
    private readonly IConfiguration _configuration;
    private readonly NavigationManager _navigationManager;
    private HubConnection hubConnection;
    private bool _isConnectionClosedCallbackSet = false;
    public async Task LogException(Exception exception)
    {
        var exceptionLog = new ExceptionLog
        {
            LogLevel = LogLevel.Error,
            Message = exception.Message,
            StackTrace = exception.StackTrace
        };
        
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync("Log", exceptionLog);
    }

    public LoggingService(ICallbackExecutor callbackExecutor, IConfiguration configuration, NavigationManager navigationManager)
    {
        _callbackExecutor = callbackExecutor;
        _configuration = configuration;
        _navigationManager = navigationManager;
        InitializeHubConnection();
        RegisterHubEventHandlers();
    }

    private void InitializeHubConnection()
    {
        hubConnection = HubServiceConnectionBuilder
            .Build(_navigationManager.ToAbsoluteUri(HubRelativeAddresses.ExceptionLoggingHubRelativeAddress));
    }

    private void RegisterHubEventHandlers()
    {
        return;
    }

    public async Task<HubConnection> GetHubConnectionAsync()
    {
        //Shortcut connection is alive and ready to be used
        if (hubConnection.State is HubConnectionState.Connected)
            return hubConnection;

        if (hubConnection == null)
            throw new ArgumentException($"{nameof(hubConnection)} was not properly instantiated.");

        while (hubConnection.State is not HubConnectionState.Connected)
        {
            try
            {
                if (hubConnection.State is not HubConnectionState.Disconnected)
                    await hubConnection.StopAsync();

                await hubConnection.StartAsync();
            }
            catch
            {
                var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                await Task.Delay(interval);
                return await GetHubConnectionAsync();
            }
        }
            
        _callbackExecutor.ExecuteSubscriptionsByName(true, "OnExceptionLoggingHubConnectionStatusChanged");

        if (_isConnectionClosedCallbackSet is false)
        {
            hubConnection.Closed += OnConnectionLost;
            _isConnectionClosedCallbackSet = true;
        }

        return hubConnection;
    }
    
    private async Task OnConnectionLost(Exception? exception)
    {
        _callbackExecutor.ExecuteSubscriptionsByName(false, "OnExceptionLoggingHubConnectionStatusChanged");
        await GetHubConnectionAsync();
    }
}