using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.Services.CloudKeyService;
using LimpShared.Encryption;
using LimpShared.Models.Message;

namespace Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling
{
    public class AESOfferHandler : IAESOfferHandler
    {
        private readonly ICryptographyService _cryptographyService;
        private readonly IBrowserKeyStorage _localKeyManager;

        public AESOfferHandler(ICryptographyService cryptographyService, IBrowserKeyStorage localKeyManager)
        {
            _cryptographyService = cryptographyService;
            _localKeyManager = localKeyManager;
        }
        public async Task<Message> GetAESOfferResponse(Message offerMessage)
        {
            string decryptedAESKey = await DecryptAESKeyFromMessage(offerMessage);

            Key aesKeyForConversation = new()
            {
                Value = decryptedAESKey,
                Contact = offerMessage.Sender,
                Format = KeyFormat.RAW,
                Type = KeyType.AES,
                Author = offerMessage.Sender
            };

            if (!string.IsNullOrWhiteSpace(offerMessage.Sender))
            {
                lock (InMemoryKeyStorage.AESKeyStorage)
                {
                    InMemoryKeyStorage.AESKeyStorage[offerMessage.Sender] = aesKeyForConversation;
                }

                await _localKeyManager.SaveInMemoryKeysInLocalStorage();
            }

            return new Message
            {
                Sender = offerMessage.TargetGroup,
                Type = MessageType.AESAccept,
                TargetGroup = offerMessage.Sender,
            };
        }

        private async Task<string> DecryptAESKeyFromMessage(Message message)
        {

            if (string.IsNullOrWhiteSpace(message.Sender))
                throw new ArgumentException($"AES offer was not containing a {nameof(Message.Sender)}");

            string decryptedAESKey = await GetDecryptedAESKeyFromMessage(message);

            return decryptedAESKey;
        }

        private async Task<string> GetDecryptedAESKeyFromMessage(Message message)
        {
            string? encryptedAESKey = message.PlainTextPayload;
            if (string.IsNullOrWhiteSpace(encryptedAESKey))
                throw new ArgumentException("AESOffer message was not containing any AES Encrypted string.");

            string? decryptedAESKey = (await _cryptographyService.DecryptAsync<RSAHandler>
                (new Cryptogramm
                {
                    Cyphertext = encryptedAESKey
                })).PlainText;

            if (string.IsNullOrWhiteSpace(decryptedAESKey))
                throw new ArgumentException("Could not decrypt an AES Key.");

            return decryptedAESKey;
        }
    }
}
