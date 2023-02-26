using Limp.Client.Cryptography.CryptoHandlers;
using Limp.Client.Cryptography.KeyStorage;
using LimpShared.Encryption;
using Microsoft.JSInterop;

namespace Limp.Client.Cryptography
{
    public class CryptographyService : ICryptographyService
    {
        private readonly IJSRuntime _jSRuntime;
        private static Action? OnAESGeneratedCallback { get; set; }

        public CryptographyService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
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
                case (KeyType.RSAPublic):
                    InMemoryKeyStorage.RSAPublic = cryptoKey;
                    break;
                case (KeyType.RSAPrivate):
                    InMemoryKeyStorage.RSAPrivate = cryptoKey;
                    break;
                case (KeyType.AES):
                    InMemoryKeyStorage.AESKeyStorage.Add(contact!, cryptoKey);
                    if(OnAESGeneratedCallback != null)
                    {
                        OnAESGeneratedCallback();
                        OnAESGeneratedCallback = null;
                    }
                    break;
                default:
                    throw new ApplicationException($"Unknown key type passed in: {nameof(cryptoKey.Type)}");
            }
        }

        public async Task GenerateRSAKeyPairAsync()
        {
            if(InMemoryKeyStorage.RSAPublic == null && InMemoryKeyStorage.RSAPrivate == null)
                await _jSRuntime.InvokeVoidAsync("GenerateRSAOAEPKeyPair");
        }
        public async Task GenerateAESKeyAsync(string contact, Action callback)
        {
            OnAESGeneratedCallback = callback;
            await _jSRuntime.InvokeVoidAsync("GenerateAESKey", contact);
        }
        public void SetAESKey(string contactName, Key key)
        {
            Key? ExistingKey = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contactName);

            if (ExistingKey is null)
                InMemoryKeyStorage.AESKeyStorage.Add(contactName, key);
            else
                InMemoryKeyStorage.AESKeyStorage[contactName] = key;
        }
        public async Task<string> DecryptAsync<T>(string text, string? contact = null) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(text, contact);
        }
        public async Task<string> EncryptAsync<T>(string text, string? contact = null) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(text, contact);
        }
    }
}
