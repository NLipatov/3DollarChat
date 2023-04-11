using ClientServerCommon.Models.Message;

namespace Limp.Client.HubInteraction.Handlers.MessageDecryption;

public interface IMessageDecryptor
{
    Task<Message> DecryptAsync(Message encryptedMessage);
}
