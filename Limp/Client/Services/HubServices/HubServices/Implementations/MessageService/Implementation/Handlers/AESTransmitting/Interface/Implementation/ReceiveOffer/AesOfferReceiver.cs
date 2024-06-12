using System.Text.Json;
using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
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
        private readonly IContactsProvider _contactsProvider;
        private readonly IJSRuntime _jsRuntime;
        private readonly IKeyStorage<AesHandler> _aesKeyStorage;
        private readonly IKeyStorage<RsaHandler> _rsaKeyStorage;

        public AesOfferReceiver(ICryptographyService cryptographyService,
            IContactsProvider contactsProvider, IJSRuntime jsRuntime, IKeyStorage<AesHandler> aesKeyStorage, IKeyStorage<RsaHandler> rsaKeyStorage)
        {
            _cryptographyService = cryptographyService;
            _contactsProvider = contactsProvider;
            _jsRuntime = jsRuntime;
            _aesKeyStorage = aesKeyStorage;
            _rsaKeyStorage = rsaKeyStorage;
        }

        public async Task<Message> ReceiveAesOfferAsync(Message offerMessage)
        {
            Key aesKey;
            try
            {
                aesKey = await GetAesKey(offerMessage);
            }
            catch (Exception e)
            {
                return new Message
                {
                    Sender = offerMessage.Target,
                    Type = MessageType.AesOfferDecline,
                    Target = offerMessage.Sender,
                };
            }

            await _aesKeyStorage.StoreAsync(new Key
            {
                Id = aesKey.Id,
                Value = aesKey.Value,
                Contact = offerMessage.Sender,
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                Author = offerMessage.Sender,
                IsAccepted = true,
                OfferMessageId = offerMessage.Id,
            });

            return new Message
            {
                Sender = offerMessage.Target,
                Type = MessageType.AesOfferAccept,
                Target = offerMessage.Sender,
                Cryptogramm = new()
                {
                    KeyId = aesKey.Id
                }
            };
        }

        private async Task<Key> GetAesKey(Message offerMessage)
        {
            var myRsaKeys = await _rsaKeyStorage.GetAsync(string.Empty, KeyType.RsaPrivate);
            var decryptedCryptogram = await _cryptographyService.DecryptAsync<RsaHandler>
            (new Cryptogram
            {
                Cyphertext = offerMessage.Cryptogramm?.Cyphertext,
                KeyId = offerMessage.Cryptogramm!.KeyId
            }, myRsaKeys.FirstOrDefault() ?? throw new ApplicationException("Missing key"));

            AesOffer? offer = JsonSerializer.Deserialize<AesOffer>(decryptedCryptogram.Cyphertext ?? string.Empty);

            if (string.IsNullOrWhiteSpace(offer?.key.Value?.ToString()))
                throw new ArgumentException("Could not decrypt an AES Key.");

            var contact = await _contactsProvider.GetContact(offerMessage.Sender ?? string.Empty, _jsRuntime);
            var contactPassPhrase = contact?.TrustedPassphrase ?? string.Empty;

            if (offer.PassPhrase != contactPassPhrase)
                throw new ArgumentException("Passphrase is not matching.");

            return offer.key;
        }
    }
}