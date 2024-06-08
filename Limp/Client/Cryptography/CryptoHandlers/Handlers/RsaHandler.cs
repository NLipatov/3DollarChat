using Client.Application.Cryptography;
using EthachatShared.Encryption;
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

        public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key)
        {
            string encryptedMessage = await _jSRuntime
                .InvokeAsync<string>("EncryptWithRSAPublicKey", cryptogram.Cyphertext, key.Value);

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

        public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, Key key)
        {
            string decryptedMessage = await _jSRuntime
                .InvokeAsync<string>("DecryptWithRSAPrivateKey", cryptogram.Cyphertext, key.Value);

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