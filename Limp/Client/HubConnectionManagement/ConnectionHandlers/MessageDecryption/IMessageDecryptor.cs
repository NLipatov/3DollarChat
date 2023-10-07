using LimpShared.Models.Message;

namespace Limp.Client.HubInteraction.Handlers.MessageDecryption;

public interface IMessageDecryptor
{
    Task<Cryptogramm> DecryptAsync(Message encryptedMessage);
}
