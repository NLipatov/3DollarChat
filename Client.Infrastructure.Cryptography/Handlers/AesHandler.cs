using Client.Application.Cryptography;
using Client.Infrastructure.Cryptography.Handlers.Models;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Client.Infrastructure.Cryptography.Handlers
{
    public class AesHandler(IRuntimeCryptographyExecutor runtimeCryptographyExecutor) : ICryptoHandler
    {
        public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, Key key)
        {
            EncryptionResult result = await runtimeCryptographyExecutor.InvokeAsync<EncryptionResult>("AESDecryptText",
            [
                cryptogram.Cyphertext ?? string.Empty,
                key.Value?.ToString() ?? throw new ApplicationException("Missing key"),
                cryptogram.Iv
            ]);

            return new()
            {
                Cyphertext = result.Ciphertext,
                Iv = result.Iv,
                KeyId = key.Id
            };
        }

        public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key)
        {
            EncryptionResult result = await runtimeCryptographyExecutor
                .InvokeAsync<EncryptionResult>("AESEncryptText",
                [
                    cryptogram.Cyphertext ?? string.Empty,
                    key.Value?.ToString() ?? throw new ApplicationException("Missing key")
                ]);

            return new()
            {
                Cyphertext = result.Ciphertext,
                Iv = result.Iv,
                KeyId = key.Id,
            };
        }
    }
}