using Client.Application.Cryptography;
using Client.Infrastructure.Cryptography.Handlers.Exceptions;
using Client.Infrastructure.Cryptography.Handlers.Models;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Client.Infrastructure.Cryptography.Handlers;

public class RsaHandler(IRuntimeCryptographyExecutor runtimeCryptographyExecutor) : ICryptoHandler
{
    public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key)
    {
        EncryptionResult result = await runtimeCryptographyExecutor
            .InvokeAsync<EncryptionResult>("EncryptWithRSAPublicKey", [cryptogram.Cyphertext, key.Value?.ToString() ?? throw new MissingKeyException()]);

        return new()
        {
            Cyphertext = result.Ciphertext
        };
    }

    public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, Key key)
    {
        EncryptionResult result = await runtimeCryptographyExecutor
            .InvokeAsync<EncryptionResult>("DecryptWithRSAPrivateKey",
                [cryptogram.Cyphertext, key.Value?.ToString() ?? throw new MissingKeyException()]);

        return new()
        {
            Cyphertext = result.Ciphertext
        };
    }
}