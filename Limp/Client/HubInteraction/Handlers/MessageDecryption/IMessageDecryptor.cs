using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;

namespace Limp.Client.HubInteraction.Handlers.MessageDecryption;

public interface IMessageDecryptor
{
    Task<Message> DecryptAsync(Message encryptedMessage);
}
