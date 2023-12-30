using LimpShared.Models.Message;

namespace Ethachat.Client.HubInteraction.Handlers.MessageDecryption;

public interface IMessageDecryptor
{
    Task<Cryptogramm> DecryptAsync(Message encryptedMessage);
}
