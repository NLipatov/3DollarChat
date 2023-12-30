using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using EthachatShared.Models.Message;

namespace Ethachat.Client.HubInteraction.Handlers.MessageDecryption;

public class MessageDecryptor : IMessageDecryptor
{
    private readonly ICryptographyService _cryptographyService;

    public MessageDecryptor(ICryptographyService cryptographyService)
    {
        _cryptographyService = cryptographyService;
    }
    public async Task<Cryptogramm> DecryptAsync(Message encryptedMessage)
    {
        if (encryptedMessage.Cryptogramm == null)
            throw new ArgumentException($"Given message {nameof(Message.Cryptogramm)} property was null.");

        return await _cryptographyService
            .DecryptAsync<AESHandler>(encryptedMessage.Cryptogramm, contact: encryptedMessage.Sender);
    }
}
