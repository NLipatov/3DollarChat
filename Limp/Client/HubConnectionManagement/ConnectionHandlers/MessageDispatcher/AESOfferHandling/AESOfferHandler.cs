using System.Text.Json;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.Services.BrowserKeyStorageService;
using Ethachat.Client.Services.ContactsProvider;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.KeyTransmition;
using Microsoft.JSInterop;

namespace Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling
{
    public class AESOfferHandler : IAESOfferHandler
    {
        private readonly ICryptographyService _cryptographyService;
        private readonly IBrowserKeyStorage _localKeyManager;
        private readonly IContactsProvider _contactsProvider;
        private readonly IJSRuntime _jsRuntime;

        public AESOfferHandler(ICryptographyService cryptographyService, IBrowserKeyStorage localKeyManager,
            IContactsProvider contactsProvider, IJSRuntime jsRuntime)
        {
            _cryptographyService = cryptographyService;
            _localKeyManager = localKeyManager;
            _contactsProvider = contactsProvider;
            _jsRuntime = jsRuntime;
        }

        public async Task<Message> GetAESOfferResponse(Message offerMessage)
        {
            string decryptedAESKey = string.Empty;
            try
            {
                decryptedAESKey = await DecryptAESKeyFromMessage(offerMessage);
            }
            catch (Exception e)
            {
                return new Message
                {
                    Sender = offerMessage.TargetGroup,
                    Type = MessageType.AesOfferDecline,
                    TargetGroup = offerMessage.Sender,
                };
            }

            Key aesKeyForConversation = new()
            {
                Value = decryptedAESKey,
                Contact = offerMessage.Sender,
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                Author = offerMessage.Sender,
                IsAccepted = true
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
                Type = MessageType.AesOfferAccept,
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
            if (string.IsNullOrWhiteSpace(message.Cryptogramm?.Cyphertext))
                throw new ArgumentException
                ($"Cannot get decrypted AES key from {typeof(Message).Name}: " +
                 $"{typeof(Message).Name} was not containing a {nameof(Message.Cryptogramm.Cyphertext)} property value.");

            string? encryptedAESOffer = message.Cryptogramm.Cyphertext;
            if (string.IsNullOrWhiteSpace(encryptedAESOffer))
                throw new ArgumentException("AESOffer message was not containing any AES Encrypted string.");

            var decryptedCryptogram = await _cryptographyService.DecryptAsync<RSAHandler>
            (new Cryptogramm
            {
                Cyphertext = encryptedAESOffer
            });

            AesOffer? offer = JsonSerializer.Deserialize<AesOffer>(decryptedCryptogram.Cyphertext ?? string.Empty);

            if (string.IsNullOrWhiteSpace(offer?.AesKey))
                throw new ArgumentException("Could not decrypt an AES Key.");

            var contact = await _contactsProvider.GetContact(message.Sender ?? string.Empty, _jsRuntime);
            var contactPassPhrase = contact?.TrustedPassphrase ?? string.Empty;

            if (offer.PassPhrase != contactPassPhrase)
                throw new ArgumentException("Passphrase is not matching.");

            return offer.AesKey;
        }
    }
}