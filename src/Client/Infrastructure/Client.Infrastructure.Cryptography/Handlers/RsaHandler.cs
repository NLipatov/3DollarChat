using Client.Application.Cryptography;
using Client.Application.Runtime;
using Client.Infrastructure.Cryptography.Handlers.Exceptions;
using Client.Infrastructure.Cryptography.Handlers.Models;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using MessagePack;

namespace Client.Infrastructure.Cryptography.Handlers;

public class RsaHandler(IPlatformRuntime platformRuntime) : ICryptoHandler
{
    public async Task<BinaryCryptogram> Encrypt<T>(T data, Key key)
    {
        var bytes = MessagePackSerializer.Serialize(data);
        var encryptedBytes = await platformRuntime.InvokeAsync<byte[]>("EncryptDataWithRSAPublicKey",
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
        var decryptedData = await platformRuntime.InvokeAsync<byte[]>("DecryptDataWithRSAPrivateKey",
            [cryptogram.Cypher, key.Value?.ToString()]);

        return new BinaryCryptogram
        {
            KeyId = cryptogram.KeyId,
            Cypher = decryptedData
        };
    }

    public async Task<TextCryptogram> Encrypt(TextCryptogram textCryptogram, Key key)
    {
        try
        {
            EncryptionResult result = await platformRuntime
                .InvokeAsync<EncryptionResult>("EncryptWithRSAPublicKey",
                    [textCryptogram.Cyphertext, key.Value?.ToString() ?? throw new MissingKeyException()]);

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

    public async Task<TextCryptogram> Decrypt(TextCryptogram textCryptogram, Key key)
    {
        EncryptionResult result = await platformRuntime
            .InvokeAsync<EncryptionResult>("DecryptWithRSAPrivateKey",
                [textCryptogram.Cyphertext, key.Value?.ToString() ?? throw new MissingKeyException()]);

        return new()
        {
            Cyphertext = result.Ciphertext
        };
    }
}