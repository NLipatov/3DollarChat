using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.ContactsProvider;
using EthachatShared.Encryption;
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
    private readonly IKeyStorage _keyStorage;

    public AesOfferSender(ICryptographyService cryptographyService, IAuthenticationHandler authenticationHandler,
        IContactsProvider contactsProvider, IJSRuntime jsRuntime, IKeyStorage keyStorage)
    {
        _cryptographyService = cryptographyService;
        _authenticationHandler = authenticationHandler;
        _contactsProvider = contactsProvider;
        _jsRuntime = jsRuntime;
        _keyStorage = keyStorage;
    }

    public async Task<AesOffer> GenerateAesOfferAsync(string partnersUsername, Key aesKey)
    {
        if (string.IsNullOrWhiteSpace(aesKey.Value?.ToString()))
            throw new ApplicationException("Could not properly generated an AES Key for conversation");

        var contact = await _contactsProvider.GetContact(partnersUsername, _jsRuntime);

        AesOffer offer = new()
        {
            Id = aesKey.Id,
            Sender = await _authenticationHandler.GetUsernameAsync(),
            Target = partnersUsername,
            key = aesKey,
            PassPhrase = contact?.TrustedPassphrase ?? string.Empty
        };

        await _keyStorage.StoreAsync(new Key
        {
            Id = aesKey.Id,
            Value = aesKey.Value,
            Contact = partnersUsername,
            Format = KeyFormat.Raw,
            Type = KeyType.Aes,
            Author = await _authenticationHandler.GetUsernameAsync(),
            IsAccepted = false,
            CreationDate = aesKey.CreationDate
        });

        return offer;
    }
}