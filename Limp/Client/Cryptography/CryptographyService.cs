using Client.Application.Cryptography;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.KeyStorageService.KeyStorage;
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
            GenerateRsaKeyPairAsync();
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
            if ((InMemoryKeyStorage.MyRSAPublic?.Value?.ToString() ?? string.Empty).Length > 0 ||
                (InMemoryKeyStorage.MyRSAPrivate?.Value?.ToString() ?? string.Empty).Length > 0 )
                return;
            
            var keyPair = await _jSRuntime.InvokeAsync<string[]>("GenerateRSAOAEPKeyPairAsync");
            var publicRsa = new Key
            {
                Value = keyPair[0],
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPublic,
                Contact = null
            };
            var privateRsa = new Key
            {
                Value = keyPair[1],
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPrivate,
                Contact = null
            };
            InMemoryKeyStorage.MyRSAPrivate = privateRsa;
            InMemoryKeyStorage.MyRSAPublic = publicRsa;
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

        public async Task<Cryptogram> DecryptAsync<T>(Cryptogram cryptogram, Key key)
            where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(cryptogram, key);
        }

        public async Task<Cryptogram> EncryptAsync<T>(Cryptogram cryptogram, Key key) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(cryptogram, key);
        }
    }
}