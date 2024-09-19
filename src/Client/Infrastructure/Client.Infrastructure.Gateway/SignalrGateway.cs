using Client.Application.Gateway;
using Client.Infrastructure.Gateway.Extensions;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Infrastructure.Gateway;

/// <summary>
/// A SignalR-compatible <see cref="IGateway"/> implementation
/// </summary>
public class SignalrGateway : IGateway
{
    private const int ReconnectionInterval = 3000;
    private Func<Task<CredentialsDTO>>? _credentialsFactory;
    private HubConnection? _connection;

    public async Task ConfigureAsync(Uri hubAddress, Func<Task<CredentialsDTO>>? credentialsFactory = null)
    {
        if (_connection != null)
            return;

        if (credentialsFactory is not null)
            _credentialsFactory = credentialsFactory;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubAddress)
            .AddMessagePackProtocol()
            .Build();

        _connection.Closed += async _ =>
        {
            Console.WriteLine("Event: Closed");
            _connection = await GetHubConnectionAsync();
        };

        _connection = await GetHubConnectionAsync();

        await AddEventCallbackAsync<string>("Authenticated", _ => Task.CompletedTask);
    }

    private async Task<HubConnection> GetHubConnectionAsync()
    {
        if (_connection is null)
            throw new NullReferenceException(
                $"{nameof(_connection)} is null. Invoke {nameof(ConfigureAsync)} before calling this method.");

        //HubConnection can only be started if state is Disconnected
        while (_connection!.State is not HubConnectionState.Connected)
        {
            try
            {
                if (_connection.State is HubConnectionState.Connected)
                    return _connection;
                
                await _connection.StopAsync();
                await _connection.StartAsync();
                if (_credentialsFactory != null)
                {
                    var credentialsDto = await _credentialsFactory();
                    await _connection.SendAsync("SetUsername", credentialsDto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR connection attempt failed: {ex.Message}");
            }
            finally
            {
                // delay between reconnection attempts
                if (_connection!.State is not HubConnectionState.Connected)
                {
                    await Task.Delay(ReconnectionInterval);
                }
            }
        }

        return _connection;
    }

    public async Task AckTransferAsync(Guid id)
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync("OnTransferAcked", id);
    }

    public async Task AddEventCallbackAsync<T>(string methodName, Func<T, Task> handler)
    {
        var connection = await GetHubConnectionAsync();
        connection.On<T>(methodName, async data => await handler.SafeInvokeAsync(data));
    }

    public async Task AddEventCallbackAsync<T1, T2>(string methodName, Func<T1, T2, Task> handler)
    {
        var connection = await GetHubConnectionAsync();
        connection.On<T1, T2>(methodName, async (t1Data, t2Data) => await handler.SafeInvokeAsync(t1Data, t2Data));
    }

    public async Task AddEventCallbackAsync(string methodName, Func<Task> handler)
    {
        var connection = await GetHubConnectionAsync();
        connection.On(methodName, async () => await handler.SafeInvokeAsync());
    }

    private async Task SendAsync(string methodName, object arg)
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync(methodName, arg);
    }

    public async Task TransferAsync(ClientToServerData data) => await SendAsync(data.EventName, data);

    public async Task TransferAsync(ClientToClientData data) => await SendAsync("TransferAsync", data);

    public async Task UnsafeTransferAsync(ClientToClientData data) => await SendAsync("TransferAsync", data);
}