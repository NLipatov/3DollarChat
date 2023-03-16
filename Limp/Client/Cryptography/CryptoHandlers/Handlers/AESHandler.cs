using ClientServerCommon.Models.Message;
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
        public async Task<Cryptogramm> Decrypt(Cryptogramm cryptogramm, string? contact = null)
        {
            if (string.IsNullOrWhiteSpace(cryptogramm.IV))
                throw new ArgumentException("Please provide an IV");

            await _jSRuntime.InvokeVoidAsync("ImportIV", cryptogramm.IV);

            Key? key = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact);
            if (key == null)
                throw new ApplicationException("RSA Public key was null");

            await _jSRuntime.InvokeVoidAsync("importSecretKey", key.Value.ToString());

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESDecryptMessage", cryptogramm.Cyphertext, key.Value.ToString());

            return new Cryptogramm
            {
                PlainText = decryptedMessage
            };
        }

        public async Task<Cryptogramm> Encrypt(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null)
        {
            string? aesKey = string.Empty;

            if (!string.IsNullOrWhiteSpace(PublicKeyToEncryptWith))
                aesKey = PublicKeyToEncryptWith;
            else if (!string.IsNullOrWhiteSpace(contact))
                aesKey = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact)?.Value?.ToString();

            if (string.IsNullOrWhiteSpace(aesKey))
                throw new ApplicationException("Could not resolve a AES key for encryption.");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("AESEncryptMessage", cryptogramm.Cyphertext, aesKey);

            string iv = await _jSRuntime.InvokeAsync<string>("ExportIV");

            return new Cryptogramm
            {
                Cyphertext = encryptedMessage,
                IV = iv,
            };
        }
    }
}
