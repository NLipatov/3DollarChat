using Limp.Client.Cryptography.KeyStorage;
using Microsoft.JSInterop;

namespace Limp.Client.Cryptography.CryptoHandlers.Handlers
{
    public class RSAHandler : ICryptoHandler
    {
        private readonly IJSRuntime _jSRuntime;

        public RSAHandler(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<string> Encrypt(string text, string? contact = null)
        {
            if (InMemoryKeyStorage.MyRSAPublic?.Value == null)
                throw new ApplicationException("RSA Public key was null");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("EncryptWithRSAPublicKey", text, InMemoryKeyStorage.MyRSAPublic.Value);

            return encryptedMessage;
        }

        public async Task<string> Decrypt(string text, string? contact = null)
        {
            if (InMemoryKeyStorage.MyRSAPrivate?.Value == null)
                throw new ApplicationException("RSA Private key was null");

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("DecryptWithRSAPrivateKey", text, InMemoryKeyStorage.MyRSAPrivate.Value);

            return decryptedMessage;
        }
    }
}
