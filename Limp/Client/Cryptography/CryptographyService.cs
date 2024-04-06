using Ethachat.Client.Cryptography.CryptoHandlers;
using Ethachat.Client.Cryptography.KeyModels;
using Ethachat.Client.Cryptography.KeyStorage;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IJSRuntime _jSRuntime;

        public CryptographyService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
            _jSRuntime.InvokeVoidAsync("GenerateRSAOAEPKeyPair");
        }

        public async Task<CompositeRsa> GenerateRsaKeyPairAsync()
        {
            if (InMemoryKeyStorage.MyRSAKey?.Value != null)
            {
                var currentRsa = InMemoryKeyStorage.MyRSAKey.Value as CompositeRsa;
                if (currentRsa is not null)
                    return currentRsa;
            }

            var rsaPair = await _jSRuntime.InvokeAsync<string[]>("GenerateRSAOAEPKeyPair");
            var compositeRsa = new CompositeRsa
            {
                PublicKey = rsaPair.First(),
                PrivateKey = rsaPair.Skip(1).First()
            };

            Key key = new Key
            {
                Value = compositeRsa,
                Contact = string.Empty,
                Format = KeyFormat.PemSpki,
                Type = KeyType.RsaComposite,
                Author = string.Empty,
                CreationDate = DateTime.UtcNow,
                IsAccepted = true
            };

            InMemoryKeyStorage.MyRSAKey = key;

            return compositeRsa;
        }

        public async Task<Key> GenerateAesKeyAsync(string contactName)
        {
            var key = new Key
            {
                Value = await _jSRuntime.InvokeAsync<string>("GenerateKey", 256, "AES-GCM"),
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                IsAccepted = false,
                CreationDate = DateTime.UtcNow,
                Contact = contactName,
            };

            InMemoryKeyStorage.AESKeyStorage.TryAdd(contactName, key);

            return key;
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