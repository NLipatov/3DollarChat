using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IRuntimeCryptographyExecutor _cryptographyExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IKeyStorage _keyStorage;

        public CryptographyService(IPlatformRuntime platformRuntime, IAuthenticationHandler authenticationHandler, IKeyStorage keyStorage)
        {
            _cryptographyExecutor = new RuntimeCryptographyExecutor(platformRuntime);
            _authenticationHandler = authenticationHandler;
            _keyStorage = keyStorage;
            _ = GenerateRsaKeyPairAsync();
        }

        private async Task GenerateRsaKeyPairAsync()
        {
            var keyPair = await _cryptographyExecutor.InvokeAsync<string[]>("GenerateRSAOAEPKeyPairAsync", []);
            var publicRsa = new Key
            {
                Value = keyPair[0],
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPublic,
                Contact = string.Empty
            };
            var privateRsa = new Key
            {
                Value = keyPair[1],
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaPrivate,
                Contact = string.Empty
            };
            await _keyStorage.StoreAsync(privateRsa);
            await _keyStorage.StoreAsync(publicRsa);
        }

        public async Task<Key> GenerateAesKeyAsync(string contact)
        {
            var key = await _cryptographyExecutor.InvokeAsync<string>("GenerateAESKeyAsync", []);
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
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _cryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(cryptogram, key);
        }

        public async Task<Cryptogram> EncryptAsync<T>(Cryptogram cryptogram, Key key) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _cryptographyExecutor);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(cryptogram, key);
        }
    }
}