using EthachatShared.Models.Message;

namespace Client.Application.Gateway;

/// <summary>
/// Communication channel between server and client
/// </summary>
public interface IGateway
{
    /// <summary>
    /// Sends data to server
    /// </summary>
    /// <param name="data">data to send</param>
    Task TransferAsync(ClientToServerData data);

    /// <summary>
    /// Sends encrypted data to other client
    /// </summary>
    /// <param name="data">data to send</param>
    Task TransferAsync(ClientToClientData data);

    /// <summary>
    /// Send unencrypted data to other client
    /// </summary>
    /// <param name="data">data to send</param>
    Task UnsafeTransferAsync(ClientToClientData data);

    /// <summary>
    /// Acks the transfer(sends transfer confirmation to server)
    /// </summary>
    /// <param name="ackData">ack data</param>
    /// <typeparam name="T">ack data type</typeparam>
    Task AckTransferAsync<T>(T ackData);

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T">argument type.</typeparam>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    Task AddEventCallbackAsync<T>(string methodName, Func<T, Task> handler);

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">argument type.</typeparam>
    /// <typeparam name="T2">argument type.</typeparam>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    Task AddEventCallbackAsync<T1, T2>(string methodName, Func<T1, T2, Task> handler);

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    Task AddEventCallbackAsync(string methodName, Func<Task> handler);

    /// <summary>
    /// Calls SendAsync on HubConnectionInstance
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="arg"></param>
    /// <returns></returns>
    Task SendAsync(string methodName, object arg);
}