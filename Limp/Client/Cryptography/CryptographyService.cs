using Ethachat.Client.Cryptography.CryptoHandlers;
using Ethachat.Client.Cryptography.KeyStorage;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IJSRuntime _jSRuntime;
        private static Action<string>? OnAesGeneratedCallback { get; set; }

        public CryptographyService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
            _jSRuntime.InvokeVoidAsync("GenerateRSAOAEPKeyPair");
        }

        private void OnKeyExtracted(string key, int format = 0, int type = 0, string? contact = null)
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
                    throw new ApplicationException($"Unsupported key type passed: {nameof(cryptoKey.Type)}");
            }
        }

        public async Task GenerateRsaKeyPairAsync()
        {
            if (InMemoryKeyStorage.MyRSAPublic != null && InMemoryKeyStorage.MyRSAPrivate != null)
                return;
            
            var publicNPrivatePem = await _jSRuntime.InvokeAsync<string[]>("GenerateRSAOAEPKeyPair");
            var publicKey = publicNPrivatePem.First();
            var privateKey = publicNPrivatePem.Skip(1).First();
            
            OnKeyExtracted(publicKey, 2, 1);
            OnKeyExtracted(privateKey, 1, 2);
        }

        public async Task<string> GenerateAesKeyAsync(string contactName)
        {
            var key = await _jSRuntime.InvokeAsync<string>("GenerateKey",256, "AES-GCM");
            
            OnKeyExtracted(key, 3, 3, contactName);
            
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