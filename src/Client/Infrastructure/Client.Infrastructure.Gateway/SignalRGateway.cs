using Client.Application.Gateway;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Infrastructure.Gateway;

/// <summary>
/// A SignalR-compatible <see cref="IGateway"/> implementation
/// </summary>
public class SignalRGateway : IGateway
{
    private Func<Task<CredentialsDTO>> _credentialsFactory;
    private HubConnection _connection;
    private bool _isConnectionClosedCallbackSet;
    private readonly int _reconnectionIntervalMs = 500;

    public async Task ConfigureAsync(Uri hubAddress, Func<Task<CredentialsDTO>> credentialsFactory)
    {
        _credentialsFactory = credentialsFactory;
        _connection = new HubConnectionBuilder()
            .WithUrl(hubAddress, options => { options.UseStatefulReconnect = true; })
            .AddMessagePackProtocol()
            .Build();
        
        
        await AddEventCallbackAsync<string>("Authenticated", id =>
        {
            return Task.CompletedTask;
        });
    }

    private async Task<HubConnection> GetHubConnectionAsync()
    {
        while (_connection.State is HubConnectionState.Disconnected)
        {
            try
            {
                var credentialsDto = await _credentialsFactory();
                await _connection.StartAsync();
                await _connection.SendAsync("SetUsername", credentialsDto);
            }
            catch
            {
                var interval = _reconnectionIntervalMs;
                await Task.Delay(interval);
                return await GetHubConnectionAsync();
            }
        }

        if (_isConnectionClosedCallbackSet is false)
        {
            _connection.Closed += OnConnectionLost;
            _isConnectionClosedCallbackSet = true;
        }

        return _connection;
    }

    private async Task OnConnectionLost(Exception? exception)
        => await GetHubConnectionAsync();

    public async Task AckTransferAsync<T>(T ackData)
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync("OnTransferAcked", ackData);
    }

    public async Task AddEventCallbackAsync<T>(string methodName, Func<T, Task> handler)
    {
        var connection = await GetHubConnectionAsync();
        connection.On<T>(methodName, async data => { await handler(data); });
    }

    public async Task AddEventCallbackAsync<T1, T2>(string methodName, Func<T1, T2, Task> handler)
    {
        var connection = await GetHubConnectionAsync();
        connection.On<T1, T2>(methodName, async (t1Data, t2Data) => { await handler(t1Data, t2Data); });
    }

    public async Task AddEventCallbackAsync(string methodName, Func<Task> handler)
    {
        var connection = await GetHubConnectionAsync();
        connection.On(methodName, async () => { await handler(); });
    }

    public async Task TransferAsync<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync("TransferAsync", data);
    }

    public async Task UnsafeTransferAsync(EncryptedDataTransfer data)
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync("TransferAsync", data);
    }
}