namespace Ethachat.Client.Services.UndecryptedMessagesService;

/// <summary>
/// Used for undecrypted messages.
/// Once AES key updated, this service will call a AskForResend method, which will ask sender to re-send those messages. 
/// </summary>
public interface IUndecryptedMessagesStorageService<T>
{
    /// <summary>
    /// Add to undecrypted (for later resend request)
    /// </summary>
    void Add(T item);
}