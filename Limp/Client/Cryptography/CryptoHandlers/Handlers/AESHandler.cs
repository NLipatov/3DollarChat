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
        public async Task<string> Decrypt(string text)
        {
            if (InMemoryKeyStorage.AES?.Value == null)
                throw new ApplicationException("RSA Public key was null");

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESDecryptMessage", text, InMemoryKeyStorage.AES.Value);

            return decryptedMessage;
        }

        public async Task<string> Encrypt(string text)
        {
            if (InMemoryKeyStorage.AES?.Value == null)
                throw new ApplicationException("RSA Public key was null");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESEncryptMessage", text, InMemoryKeyStorage.AES.Value);
            return encryptedMessage;
        }
    }
}
