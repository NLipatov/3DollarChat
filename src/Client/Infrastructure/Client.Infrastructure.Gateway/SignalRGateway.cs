using Client.Application.Gateway;
using Client.Infrastructure.Gateway.ClientToClient;
using Client.Infrastructure.Gateway.ClientToServer;
using EthachatShared.Contracts;
using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Message;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Infrastructure.Gateway;

/// <summary>
/// A SignalR-compatible <see cref="IGateway"/> implementation
/// </summary>
public class SignalRGateway : IGateway, IRawSendAsyncProvider
{
    private Func<Task<CredentialsDTO>>? _credentialsFactory;
    private HubConnection? _connection;
    private bool _isConnectionClosedCallbackSet;
    private readonly int _reconnectionIntervalMs = 500;
    private IReliableSender<ClientToClientData> ReliableContainerSender => new ClientTransferContainerReliableSender(this);
    private IReliableSender<ClientToServerData> ReliableServerMessageSender => new ServerMessageReliableSender(this);

    public async Task ConfigureAsync(Uri hubAddress, Func<Task<CredentialsDTO>> credentialsFactory)
    {
        _credentialsFactory = credentialsFactory;
        _connection = new HubConnectionBuilder()
            .WithUrl(hubAddress, options => { options.UseStatefulReconnect = true; })
            .AddMessagePackProtocol()
            .Build();


        await AddEventCallbackAsync<string>("Authenticated", _ => Task.CompletedTask);
        await AddEventCallbackAsync<Guid>("OnClientToClientDataAck", id =>
        {
            ReliableContainerSender.OnAck(id);
            return Task.CompletedTask;
        });
        await AddEventCallbackAsync<Guid>("OnClientToServerDataAck", guid =>
        {
            ReliableServerMessageSender.OnAck(guid);
            return Task.CompletedTask;
        });
    }

    public async Task ConfigureAsync(Uri hubAddress)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubAddress, options => { options.UseStatefulReconnect = true; })
            .AddMessagePackProtocol()
            .Build();


        await AddEventCallbackAsync<string>("Authenticated", _ => Task.CompletedTask);
    }

    private async Task<HubConnection> GetHubConnectionAsync()
    {
        if (_connection is null)
            throw new NullReferenceException($"{nameof(_connection)} is null");

        while (_connection.State is HubConnectionState.Disconnected)
        {
            try
            {
                await _connection.StartAsync();
                if (_credentialsFactory != null)
                {
                    var credentialsDto = await _credentialsFactory();
                    await _connection.SendAsync("SetUsername", credentialsDto);
                }
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

    public async Task SendAsync(string methodName, object arg)
    {
        var connection = await GetHubConnectionAsync();
        await connection.SendAsync(methodName, arg);
    }

    public async Task TransferAsync(ClientToServerData data) => await ReliableServerMessageSender.EnqueueAsync(data);
    
    public async Task TransferAsync(ClientToClientData data) => await ReliableContainerSender.EnqueueAsync(data);

    public async Task UnsafeTransferAsync(ClientToClientData data) => await ReliableContainerSender.EnqueueAsync(data);
}