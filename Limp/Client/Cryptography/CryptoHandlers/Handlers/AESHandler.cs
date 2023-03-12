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
        public async Task<string> Decrypt(string text, string? contact = null, string? IV = null)
        {
            if (string.IsNullOrWhiteSpace(IV))
                throw new ArgumentException("Please provide an IV");

            await _jSRuntime.InvokeVoidAsync("importSecretKey", IV);

            Key? key = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact);
            if (key == null)
                throw new ApplicationException("RSA Public key was null");

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESDecryptMessage", text, key.Value.ToString());

            return decryptedMessage;
        }

        public async Task<string> Encrypt(string text, string? contact = null, string? PublicKeyToEncryptWith = null)
        {
            string? aesKey = string.Empty;

            if (!string.IsNullOrWhiteSpace(PublicKeyToEncryptWith))
                aesKey = PublicKeyToEncryptWith;
            else if (!string.IsNullOrWhiteSpace(contact))
                aesKey = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact)?.Value?.ToString();

            if (string.IsNullOrWhiteSpace(aesKey))
                throw new ApplicationException("Could not resolve a AES key for encryption.");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESEncryptMessage", text, aesKey);

            string iv = await _jSRuntime.InvokeAsync<string>("ExportIV");
            await Console.Out.WriteLineAsync($"Got IV along with cyphertext: {iv}");

            return encryptedMessage;
        }
    }
}
