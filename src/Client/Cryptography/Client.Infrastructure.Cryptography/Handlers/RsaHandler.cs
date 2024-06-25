using Client.Application.Cryptography;
using Client.Infrastructure.Cryptography.Handlers.Exceptions;
using Client.Infrastructure.Cryptography.Handlers.Models;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using MessagePack;

namespace Client.Infrastructure.Cryptography.Handlers;

public class RsaHandler(IRuntimeCryptographyExecutor runtimeCryptographyExecutor) : ICryptoHandler
{
    public async Task<BinaryCryptogram> Encrypt<T>(T data, Key key)
    {
        var bytes = MessagePackSerializer.Serialize(data);
        var encryptedBytes = await runtimeCryptographyExecutor.InvokeAsync<byte[]>("EncryptDataWithRSAPublicKey",
            [bytes, key.Value?.ToString()]);

        return new BinaryCryptogram
        {
            Cypher = encryptedBytes,
            KeyId = key.Id,
            EncryptionKeyType = key.Type.HasValue
                ? key.Type.Value
                : throw new ArgumentException($"Unexpected {nameof(key.Type)}")
        };
    }

    public async Task<BinaryCryptogram> Decrypt(BinaryCryptogram cryptogram, Key key)
    {
        var decryptedData = await runtimeCryptographyExecutor.InvokeAsync<byte[]>("DecryptDataWithRSAPrivateKey",
            [cryptogram.Cypher, key.Value?.ToString()]);

        return new BinaryCryptogram
        {
            KeyId = cryptogram.KeyId,
            Cypher = decryptedData
        };
    }

    public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key)
    {
        try
        {
            EncryptionResult result = await runtimeCryptographyExecutor
                .InvokeAsync<EncryptionResult>("EncryptWithRSAPublicKey",
                    [cryptogram.Cyphertext, key.Value?.ToString() ?? throw new MissingKeyException()]);

            return new()
            {
                Cyphertext = result.Ciphertext
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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