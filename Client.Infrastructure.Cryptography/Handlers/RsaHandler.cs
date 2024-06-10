using Client.Application.Cryptography;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Client.Infrastructure.Cryptography.Handlers
{
    public class RsaHandler(IRuntimeCryptographyExecutor runtimeCryptographyExecutor) : ICryptoHandler
    {
        public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key)
        {
            string encryptedMessage = await runtimeCryptographyExecutor
                .InvokeAsync<string>("EncryptWithRSAPublicKey", [cryptogram.Cyphertext, key.Value]);

            if (string.IsNullOrWhiteSpace(encryptedMessage))
                throw new ApplicationException("Could not encrypt text.");

            var result = new Cryptogram
            {
                Cyphertext = encryptedMessage,
                Iv = await runtimeCryptographyExecutor.InvokeAsync<string>("ExportIV", [cryptogram.Cyphertext]),
            };

            await runtimeCryptographyExecutor.InvokeVoidAsync("DeleteIv", [cryptogram.Cyphertext]);

            return result;
        }

        public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, Key key)
        {
            string decryptedMessage = await runtimeCryptographyExecutor
                .InvokeAsync<string>("DecryptWithRSAPrivateKey", [cryptogram.Cyphertext, key.Value]);

            var result = new Cryptogram()
            {
                Cyphertext = decryptedMessage,
                Iv = await runtimeCryptographyExecutor.InvokeAsync<string>("ExportIV", [cryptogram.Cyphertext]),
            };

            await runtimeCryptographyExecutor.InvokeVoidAsync("DeleteIv", [cryptogram.Cyphertext]);

            return result;
        }
    }
}