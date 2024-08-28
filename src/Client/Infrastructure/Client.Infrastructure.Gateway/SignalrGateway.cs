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
    private Func<Task<CredentialsDTO>>? _credentialsFactory;
    private HubConnection? _connection;

    public async Task ConfigureAsync(Uri hubAddress, Func<Task<CredentialsDTO>>? credentialsFactory = null)
    {
        if (_connection != null)
            return;

        if (credentialsFactory is not null)
            _credentialsFactory = credentialsFactory;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubAddress, options => { options.UseStatefulReconnect = true; })
            .AddMessagePackProtocol()
            .Build();

        _connection.Closed += async (_) =>
        {
            Console.WriteLine("Event: Closed");
            _connection = await GetHubConnectionAsync();
        };

        _connection.Reconnected += id =>
        {
            Console.WriteLine($"Event: Reconnected: {id}");
            return Task.CompletedTask;
        };

        _connection.Reconnecting += id =>
        {
            Console.WriteLine($"Event: Reconnecting: {id}");
            return Task.CompletedTask;
        };

        _connection = await GetHubConnectionAsync();

        await AddEventCallbackAsync<string>("Authenticated", _ => Task.CompletedTask);
    }

    private async Task<HubConnection> GetHubConnectionAsync()
    {
        if (_connection is null)
            throw new NullReferenceException($"{nameof(_connection)} is null");

        //HubConnection can only be started if state is Disconnected
        while (_connection.State is not HubConnectionState.Connected)
        {
            Console.WriteLine("Trying to connect...");
            try
            {
                Console.WriteLine($"State: {_connection.State}. Stopping.");
                await _connection.StopAsync();
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR connection attempt failed: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Delay between connections attempts.");
                await Task.Delay(1000);
                Console.WriteLine("Next attempt starts now.");
            }

            Console.WriteLine($"State: {_connection.State}");
        }

        return _connection;
    }

    private async Task ConnectAsync()
    {
        if (_connection is null)
            throw new NullReferenceException($"{nameof(_connection)} is null");

        Console.WriteLine("StartAsync...");
        await _connection.StartAsync();
        if (_credentialsFactory != null)
        {
            var credentialsDto = await _credentialsFactory();
            Console.WriteLine("Send credentials...");
            await _connection.SendAsync("SetUsername", credentialsDto);
        }
    }

    public async Task AckTransferAsync<T>(T ackData)
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync("OnTransferAcked", ackData);
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