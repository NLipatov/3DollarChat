namespace Client.Infrastructure.Gateway;

internal interface IRawSendAsyncProvider
{
    /// <summary>
    /// Calls SendAsync on HubConnectionInstance
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="arg"></param>
    /// <returns></returns>
    Task SendAsync(string methodName, object arg);
}