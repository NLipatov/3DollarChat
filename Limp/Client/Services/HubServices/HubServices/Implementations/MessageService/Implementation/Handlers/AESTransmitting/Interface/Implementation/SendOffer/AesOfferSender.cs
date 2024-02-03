using System.Text.Json;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.BrowserKeyStorageService;
using Ethachat.Client.Services.ContactsProvider;
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
    private readonly IBrowserKeyStorage _browserKeyStorage;
    private readonly IJSRuntime _jsRuntime;

    public AesOfferSender(ICryptographyService cryptographyService, IAuthenticationHandler authenticationHandler,
        IContactsProvider contactsProvider, IBrowserKeyStorage browserKeyStorage, IJSRuntime jsRuntime)
    {
        _cryptographyService = cryptographyService;
        _authenticationHandler = authenticationHandler;
        _contactsProvider = contactsProvider;
        _browserKeyStorage = browserKeyStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task<Message> SendAesOfferAsync(string partnersUsername, string partnersPublicKey, string aesKey)
    {
        InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.CreationDate =
            DateTime.UtcNow;
        InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value =
            aesKey;
        await _browserKeyStorage.SaveInMemoryKeysInLocalStorage();
        string? offeredAESKeyForConversation =
            InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value!.ToString();

        if (string.IsNullOrWhiteSpace(offeredAESKeyForConversation))
            throw new ApplicationException("Could not properly generated an AES Key for conversation");

        var contact = await _contactsProvider.GetContact(partnersUsername, _jsRuntime);

        AesOffer offer = new()
        {
            AesKey = offeredAESKeyForConversation,
            PassPhrase = contact?.TrustedPassphrase ?? string.Empty
        };

        string? encryptedOffer = (await _cryptographyService
            .EncryptAsync<RSAHandler>
            (new Cryptogramm { Cyphertext = JsonSerializer.Serialize(offer) },
                //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                publicKeyToEncryptWith: partnersPublicKey)).Cyphertext;

        Message messageWithAesOffer = new()
        {
            Type = MessageType.AesOffer,
            DateSent = DateTime.UtcNow,
            Sender = await _authenticationHandler.GetUsernameAsync(),
            TargetGroup = partnersUsername,
            Cryptogramm = new()
            {
                Cyphertext = encryptedOffer,
            }
        };

        return messageWithAesOffer;
    }
}