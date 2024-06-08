using Client.Application.Cryptography;
using Ethachat.Client.Services.KeyStorageService.KeyStorage;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography.CryptoHandlers.Handlers
{
    public class RsaHandler : ICryptoHandler
    {
        private readonly IJSRuntime _jSRuntime;

        public RsaHandler(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, string? contact = null, string? publicKeyToEncryptWith = null)
        {
            if (string.IsNullOrWhiteSpace(publicKeyToEncryptWith))
                throw new ArgumentException("Please provide an RSA Key to Encrypt your text with.");

            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("EncryptWithRSAPublicKey", cryptogram.Cyphertext, publicKeyToEncryptWith);

            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ApplicationException("Could not encrypt text.");

            var result = new Cryptogram
            {
                Cyphertext = encryptedMessage,
                Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogram.Cyphertext),
            };
            
            await _jSRuntime.InvokeVoidAsync("DeleteIv", cryptogram.Cyphertext);

            return result;
        }

        public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, string? contact = null)
        {
            if (InMemoryKeyStorage.MyRSAPrivate?.Value == null)
                throw new ApplicationException("RSA Private key was null");

            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("DecryptWithRSAPrivateKey", cryptogram.Cyphertext, InMemoryKeyStorage.MyRSAPrivate.Value);

            var result = new Cryptogram()
            {
                Cyphertext = decryptedMessage,
                Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogram.Cyphertext),
            };
            
            await _jSRuntime.InvokeVoidAsync("DeleteIv", cryptogram.Cyphertext);

            return result;
        }
    }
}
