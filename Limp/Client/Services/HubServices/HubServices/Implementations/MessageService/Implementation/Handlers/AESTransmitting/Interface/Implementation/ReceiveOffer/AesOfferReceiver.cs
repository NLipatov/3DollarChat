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

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.ReceiveOffer
{
    public class AesOfferReceiver : IAesOfferReceiver
    {
        private readonly ICryptographyService _cryptographyService;
        private readonly IBrowserKeyStorage _localKeyManager;
        private readonly IContactsProvider _contactsProvider;
        private readonly IJSRuntime _jsRuntime;

        public AesOfferReceiver(ICryptographyService cryptographyService, IBrowserKeyStorage localKeyManager,
            IContactsProvider contactsProvider, IJSRuntime jsRuntime)
        {
            _cryptographyService = cryptographyService;
            _localKeyManager = localKeyManager;
            _contactsProvider = contactsProvider;
            _jsRuntime = jsRuntime;
        }

        public async Task<Message> ReceiveAesOfferAsync(Message offerMessage)
        {
            string aesKey;
            try
            {
                aesKey = await GetAesKey(offerMessage);
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

            await SaveKey(aesKey, offerMessage);

            return new Message
            {
                Sender = offerMessage.TargetGroup,
                Type = MessageType.AesOfferAccept,
                TargetGroup = offerMessage.Sender,
            };
        }

        private async Task<string> GetAesKey(Message offerMessage)
        {
            var decryptedCryptogram = await _cryptographyService.DecryptAsync<RSAHandler>
            (new Cryptogramm
            {
                Cyphertext = offerMessage.Cryptogramm?.Cyphertext
            });

            AesOffer? offer = JsonSerializer.Deserialize<AesOffer>(decryptedCryptogram.Cyphertext ?? string.Empty);

            if (string.IsNullOrWhiteSpace(offer?.AesKey))
                throw new ArgumentException("Could not decrypt an AES Key.");

            var contact = await _contactsProvider.GetContact(offerMessage.Sender ?? string.Empty, _jsRuntime);
            var contactPassPhrase = contact?.TrustedPassphrase ?? string.Empty;

            if (offer.PassPhrase != contactPassPhrase)
                throw new ArgumentException("Passphrase is not matching.");

            return offer.AesKey;
        }

        private async Task SaveKey(string aesKey, Message offerMessage)
        {
            Key key = new Key()
            {
                Value = aesKey,
                Contact = offerMessage.Sender,
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                Author = offerMessage.Sender,
                IsAccepted = true,
                OfferMessageId = offerMessage.Id
            };

            InMemoryKeyStorage.AESKeyStorage[offerMessage.Sender!] = key;
            await _localKeyManager.SaveInMemoryKeysInLocalStorage();
        }
    }
}