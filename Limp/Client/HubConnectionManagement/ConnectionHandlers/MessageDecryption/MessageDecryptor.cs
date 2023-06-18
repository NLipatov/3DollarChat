using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using LimpShared.Models.Message;

namespace Limp.Client.HubInteraction.Handlers.MessageDecryption;

public class MessageDecryptor : IMessageDecryptor
{
    private readonly ICryptographyService _cryptographyService;

    public MessageDecryptor(ICryptographyService cryptographyService)
    {
        _cryptographyService = cryptographyService;
    }
    public async Task<Message> DecryptAsync(Message encryptedMessage)
    {
        if (encryptedMessage.Cryptogramm == null)
            throw new ArgumentException($"Given message {nameof(Message.Cryptogramm)} property was null.");

        encryptedMessage.Cryptogramm = await _cryptographyService
            .DecryptAsync<AESHandler>(encryptedMessage.Cryptogramm, contact: encryptedMessage.Sender);

        encryptedMessage.PlainTextPayload = encryptedMessage.Cryptogramm.PlainText;
        return encryptedMessage;
    }
}
