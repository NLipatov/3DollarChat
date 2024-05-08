using Ethachat.Client.Cryptography.CryptoHandlers;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IJSRuntime _jSRuntime;
        private readonly IAuthenticationHandler _authenticationHandler;
        private static Action<string>? OnAesGeneratedCallback { get; set; }

        public CryptographyService(IJSRuntime jSRuntime, IAuthenticationHandler authenticationHandler)
        {
            _jSRuntime = jSRuntime;
            _authenticationHandler = authenticationHandler;
            _jSRuntime.InvokeVoidAsync("GenerateRSAOAEPKeyPair");
        }

        [JSInvokable]
        public static async void OnKeyExtracted(string key, int format = 0, int type = 0, string? contact = null)
        {
            Key cryptoKey = new Key()
            {
                Value = key,
                Format = (KeyFormat)format,
                Type = (KeyType)type,
                Contact = contact
            };

            switch (cryptoKey.Type)
            {
                case (KeyType.RsaPublic):
                    InMemoryKeyStorage.MyRSAPublic = cryptoKey;
                    break;
                case (KeyType.RsaPrivate):
                    InMemoryKeyStorage.MyRSAPrivate = cryptoKey;
                    break;
                case (KeyType.Aes):
                    InMemoryKeyStorage.AESKeyStorage.TryAdd(contact!, cryptoKey);
                    if (OnAesGeneratedCallback != null)
                    {
                        OnAesGeneratedCallback(cryptoKey.Value.ToString() 
                                               ?? throw new ArgumentException
                                                   ("Cryptography key was not well formed."));
                        OnAesGeneratedCallback = null;
                    }

                    break;
                default:
                    throw new ApplicationException($"Unknown key type passed in: {nameof(cryptoKey.Type)}");
            }

            if (InMemoryKeyStorage.MyRSAPublic?.Value != null && InMemoryKeyStorage.MyRSAPrivate?.Value != null)
                KeysGeneratedHandler.CallOnKeysGenerated();
        }

        public async Task GenerateRsaKeyPairAsync()
        {
            if (InMemoryKeyStorage.MyRSAPublic == null && InMemoryKeyStorage.MyRSAPrivate == null)
                await _jSRuntime.InvokeVoidAsync("GenerateRSAOAEPKeyPair");
        }

        public async Task<Key> GenerateAesKeyAsync(string contact)
        {
            var key = await _jSRuntime.InvokeAsync<string>("GenerateAESKeyAsync");
            return new Key
            {
                Value = key,
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                Author = await _authenticationHandler.GetUsernameAsync(),
                Contact = contact,
                CreationDate = DateTime.UtcNow,
                IsAccepted = false
            };
        }

        public async Task<Cryptogramm> DecryptAsync<T>(Cryptogramm cryptogram, string? contact = null)
            where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(cryptogram, contact);
        }

        public async Task<Cryptogramm> EncryptAsync<T>(Cryptogramm cryptogram, string? contact = null,
            string? publicKeyToEncryptWith = null) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(cryptogram, contact, publicKeyToEncryptWith);
        }
    }
}