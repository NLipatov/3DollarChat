using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.Cryptography;
using LimpShared.Encryption;

namespace Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling
{
    public class AESOfferHandler : IAESOfferHandler
    {
        private readonly ICryptographyService _cryptographyService;

        public AESOfferHandler(ICryptographyService cryptographyService)
        {
            _cryptographyService = cryptographyService;
        }
        public async Task<Message> GetAESOfferResponse(Message offerMessage)
        {
            if (string.IsNullOrWhiteSpace(offerMessage.Sender))
                throw new ArgumentException($"AES offer was not containing a {nameof(Message.Sender)}");

            string decryptedAESKey = await GetDecryptedAESKeyFromMessage(offerMessage);

            await Console.Out.WriteLineAsync($"Decrypted AES: {decryptedAESKey}");

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
                lock(InMemoryKeyStorage.AESKeyStorage)
                {
                    InMemoryKeyStorage.AESKeyStorage[offerMessage.Sender] = aesKeyForConversation;
                }

                await Console.Out.WriteLineAsync($"Added an AES key for {offerMessage.Sender}");
                await Console.Out.WriteLineAsync($"Key value: {InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == offerMessage.Sender).Value.Value.ToString()}");
            }

            return new Message
            {
                Sender = offerMessage.TargetGroup,
                Type = MessageType.AESAccept,
                TargetGroup = offerMessage.Sender,
            };
        }

        private async Task<string> GetDecryptedAESKeyFromMessage(Message message)
        {
            string? encryptedAESKey = message.Payload;
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
