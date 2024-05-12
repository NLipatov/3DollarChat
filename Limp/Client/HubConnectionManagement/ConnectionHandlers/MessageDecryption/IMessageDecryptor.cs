using EthachatShared.Models.Message;

namespace Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDecryption;

public interface IMessageDecryptor
{
    Task<Cryptogram> DecryptAsync(Message encryptedMessage);
}
