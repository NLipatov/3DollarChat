using Limp.Client.Cryptography.KeyStorage;
using LimpShared.Encryption;
using Microsoft.JSInterop;

namespace Limp.Client.Cryptography.CryptoHandlers.Handlers
{
    public class AESHandler : ICryptoHandler
    {
        private readonly IJSRuntime _jSRuntime;

        public AESHandler(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<string> Decrypt(string text, string? contact = null)
        {
            Key? key = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact);
            if (key == null)
                throw new ApplicationException("RSA Public key was null");

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESDecryptMessage", text, key.Value);

            return decryptedMessage;
        }

        public async Task<string> Encrypt(string text, string? contact = null, string? PublicKeyToEncryptWith = null)
        {
            Key? key = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact);
            if (key == null)
                throw new ApplicationException("RSA Public key was null");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESEncryptMessage", text, key.Value);
            return encryptedMessage;
        }
    }
}
