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
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();

        _connection.Reconnected += async _ => await AuthenticateAsync();

        await AddEventCallbackAsync<string>("Authenticated", _ => Task.CompletedTask);
    }

    private async Task<HubConnection> GetHubConnectionAsync()
    {
        if (_connection is null)
            throw new NullReferenceException($"{nameof(_connection)} is null");

        if (_connection.State == HubConnectionState.Disconnected || _connection.State == HubConnectionState.Reconnecting)
        {
            try
            {
                await _connection.StartAsync();
                await AuthenticateAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR connection attempt failed: {ex.Message}");
            }
        }

        return _connection;
    }

    private async Task AuthenticateAsync()
    {
        if (_connection is null)
            throw new NullReferenceException($"{nameof(_connection)} is null");

        if (_credentialsFactory != null)
        {
            var credentialsDto = await _credentialsFactory();
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