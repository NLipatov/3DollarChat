using Limp.Client.Cryptography.CryptoHandlers;
using LimpShared.Encryption;
using Microsoft.JSInterop;

namespace Limp.Client.Cryptography
{
    public class CryptographyService
    {
        private readonly IJSRuntime _jSRuntime;

        public CryptographyService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        [JSInvokable]
        public static void OnKeyExtracted(string key, int format = 0, int type = 0)
        {
            Key cryptoKey = new Key()
            {
                Value = key,
                Format = (KeyFormat)format,
                Type = (KeyType)type
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
                    InMemoryKeyStorage.AES = cryptoKey;
                    break;
                default:
                    throw new ApplicationException($"Unknown key type passed in: {nameof(cryptoKey.Type)}");
            }
        }

        public async Task GenerateRSAKeyPairAsync()
        {
            await _jSRuntime.InvokeVoidAsync("GenerateRSAOAEPKeyPair");
        }
        public async Task GenerateAESKeyAsync()
        {
            await _jSRuntime.InvokeVoidAsync("GenerateAESKey");
        }
        public async Task<string> DecryptAsync<T>(string text) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T), _jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Decrypt(text);
        }
        public async Task<string> EncryptAsync<T>(string text) where T : ICryptoHandler
        {
            ICryptoHandler? cryptoHandler = (T?)Activator.CreateInstance(typeof(T),_jSRuntime);
            if (cryptoHandler is null)
                throw new ApplicationException($"Could not create a proper {typeof(T)} instance.");

            return await cryptoHandler.Encrypt(text);
        }
    }
}
