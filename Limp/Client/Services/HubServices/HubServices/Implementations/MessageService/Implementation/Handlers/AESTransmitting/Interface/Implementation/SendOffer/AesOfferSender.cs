using System.Text.Json;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.ContactsProvider;
using Ethachat.Client.Services.KeyStorageService.Implementations;
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

    public AesOfferSender(ICryptographyService cryptographyService, IAuthenticationHandler authenticationHandler,
        IContactsProvider contactsProvider, IJSRuntime jsRuntime)
    {
        _cryptographyService = cryptographyService;
        _authenticationHandler = authenticationHandler;
        _contactsProvider = contactsProvider;
        _jsRuntime = jsRuntime;
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

        string? encryptedOffer = (await _cryptographyService
            .EncryptAsync<RSAHandler>
            (new Cryptogram { Cyphertext = JsonSerializer.Serialize(offer) },
                //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                publicKeyToEncryptWith: partnersPublicKey)).Cyphertext;

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
        
        var keyStorage = new LocalStorageKeyStorage(_jsRuntime);
        await keyStorage.StoreAsync(new Key
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