using Client.Application.Cryptography;
using Client.Infrastructure.Cryptography;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.KeyStorageService.KeyStorage;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IRuntimeCryptographyExecutor CryptographyExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private static Action<string>? OnAesGeneratedCallback { get; set; }

        public CryptographyService(IJSRuntime jSRuntime, IAuthenticationHandler authenticationHandler)
        {
            CryptographyExecutor = new RuntimeCryptographyExecutor(jSRuntime);
            _authenticationHandler = authenticationHandler;
            GenerateRsaKeyPairAsync();
        }

        public async Task GenerateRsaKeyPairAsync()
        {
            if ((InMemoryKeyStorage.MyRSAPublic?.Value?.ToString() ?? string.Empty).Length > 0 ||
                (InMemoryKeyStorage.MyRSAPrivate?.Value?.ToString() ?? string.Empty).Length > 0 )
                return;
            
            var keyPair = await CryptographyExecutor.InvokeAsync<string[]>("GenerateRSAOAEPKeyPairAsync", []);
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
            var key = await CryptographyExecutor.InvokeAsync<string>("GenerateAESKeyAsync", []);
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
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), CryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(cryptogram, key);
        }

        public async Task<Cryptogram> EncryptAsync<T>(Cryptogram cryptogram, Key key) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), CryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(cryptogram, key);
        }
    }
}