using EthachatShared.Models.Authentication.Models.Credentials.CredentialsDTO;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.Interfaces;

namespace Client.Application.Gateway;

/// <summary>
/// Abstraction over SignalR hub connection
/// </summary>
public interface ISignalRGateway
{
    Task Authenticate(Uri hubAddress, CredentialsDTO credentialsDto);
    
    /// <summary>
    /// Sends encrypted data
    /// </summary>
    /// <param name="data">data to send</param>
    /// <typeparam name="T">data type</typeparam>
    Task TransferAsync<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable;
    
    /// <summary>
    /// Send unencrypted data
    /// </summary>
    /// <param name="data">data to send</param>
    Task UnsafeTransferAsync(EncryptedDataTransfer data);

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
}