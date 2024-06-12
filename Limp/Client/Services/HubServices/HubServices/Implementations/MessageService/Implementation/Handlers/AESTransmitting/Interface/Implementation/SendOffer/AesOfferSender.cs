using System.Text.Json;
using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.ContactsProvider;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.KeyTransmition;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface.Implementation.SendOffer;

public class AesOfferSender : IAesOfferSender
{
    private readonly ICryptographyService _cryptographyService;
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly IContactsProvider _contactsProvider;
    private readonly IJSRuntime _jsRuntime;
    private readonly IKeyStorage<AesHandler> _aesKeyStorage;
    private readonly IKeyStorage<RsaHandler> _rsaKeyStorage;

    public AesOfferSender(ICryptographyService cryptographyService, IAuthenticationHandler authenticationHandler,
        IContactsProvider contactsProvider, IJSRuntime jsRuntime, IKeyStorage<AesHandler> aesKeyStorage,
        IKeyStorage<RsaHandler> rsaKeyStorage)
    {
        _cryptographyService = cryptographyService;
        _authenticationHandler = authenticationHandler;
        _contactsProvider = contactsProvider;
        _jsRuntime = jsRuntime;
        _aesKeyStorage = aesKeyStorage;
        _rsaKeyStorage = rsaKeyStorage;
    }

    public async Task<Message> GenerateAesOfferAsync(string partnersUsername, string partnersPublicKey, Key aesKey)
    {
        if (string.IsNullOrWhiteSpace(aesKey.Value?.ToString()))
            throw new ApplicationException("Could not properly generated an AES Key for conversation");

        var contact = await _contactsProvider.GetContact(partnersUsername, _jsRuntime);

        AesOffer offer = new()
        {
            key = aesKey,
            PassPhrase = contact?.TrustedPassphrase ?? string.Empty
        };

        var partnerPublicRsaKey = await _rsaKeyStorage.GetAsync(partnersUsername, KeyType.RsaPublic);
        string? encryptedOffer = (await _cryptographyService
                .EncryptAsync<RsaHandler>
                (new Cryptogram
                    {
                        Cyphertext = JsonSerializer.Serialize(offer)
                    },
                    //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                    partnerPublicRsaKey.First()))
            .Cyphertext;

        Message messageWithAesOffer = new()
        {
            Type = MessageType.AesOffer,
            DateSent = DateTime.UtcNow,
            Sender = await _authenticationHandler.GetUsernameAsync(),
            Target = partnersUsername,
            Cryptogramm = new()
            {
                Cyphertext = encryptedOffer,
                KeyId = aesKey.Id
            }
        };

        await _aesKeyStorage.StoreAsync(new Key
        {
            Id = aesKey.Id,
            Value = aesKey.Value,
            Contact = messageWithAesOffer.Target,
            Format = KeyFormat.Raw,
            Type = KeyType.Aes,
            Author = messageWithAesOffer.Sender,
            IsAccepted = false,
            OfferMessageId = messageWithAesOffer.Id,
            CreationDate = aesKey.CreationDate
        });

        return messageWithAesOffer;
    }
}