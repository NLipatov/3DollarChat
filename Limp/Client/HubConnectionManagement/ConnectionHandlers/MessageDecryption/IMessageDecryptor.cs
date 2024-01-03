using EthachatShared.Models.Message;

namespace Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDecryption;

public interface IMessageDecryptor
{
    Task<Cryptogramm> DecryptAsync(Message encryptedMessage);
}
