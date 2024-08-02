using Client.Application.Cryptography;
using Client.Application.Runtime;
using Client.Infrastructure.Cryptography.Handlers.Exceptions;
using Client.Infrastructure.Cryptography.Handlers.Models;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;
using MessagePack;

namespace Client.Infrastructure.Cryptography.Handlers;

public class AesHandler(IPlatformRuntime platformRuntime) : ICryptoHandler
{
    public async Task<BinaryCryptogram> Encrypt<T>(T data, Key key)
    {
        var bytes = MessagePackSerializer.Serialize(data);
        var encryptedData =
            await platformRuntime.InvokeAsync<byte[]>("AESEncryptData", [bytes, key.Value?.ToString()]);

        var cryptogram = EncryptedBytesToCryptogram(encryptedData, key);
        return cryptogram;
    }

    public async Task<BinaryCryptogram> Decrypt(BinaryCryptogram cryptogram, Key key)
    {
        var decryptedData =
            await platformRuntime.InvokeAsync<byte[]>("AESDecryptData",
                [cryptogram.Cypher, key.Value?.ToString(), cryptogram.Iv]);

        return new BinaryCryptogram
        {
            Iv = cryptogram.Iv,
            KeyId = cryptogram.KeyId,
            Cypher = decryptedData
        };
    }

    private BinaryCryptogram EncryptedBytesToCryptogram(byte[] bytes, Key key)
    {
        var ivLength = bytes.First();
        var iv = bytes.Skip(1).Take(ivLength).ToArray();
        var encryptedData = bytes.Skip(1 + ivLength).ToArray();

        return new BinaryCryptogram
        {
            Iv = iv,
            Cypher = encryptedData,
            EncryptionKeyType = KeyType.Aes,
            KeyId = key.Id
        };
    }

    public async Task<TextCryptogram> Decrypt(TextCryptogram textCryptogram, Key key)
    {
        EncryptionResult result = await platformRuntime.InvokeAsync<EncryptionResult>("AESDecryptText",
        [
            textCryptogram.Cyphertext ?? string.Empty,
            key.Value?.ToString() ?? throw new MissingKeyException(),
            textCryptogram.Iv
        ]);

        return new()
        {
            Cyphertext = result.Ciphertext,
            Iv = result.Iv,
            KeyId = key.Id
        };
    }

    public async Task<TextCryptogram> Encrypt(TextCryptogram textCryptogram, Key key)
    {
        EncryptionResult result = await platformRuntime
            .InvokeAsync<EncryptionResult>("AESEncryptText",
            [
                textCryptogram.Cyphertext ?? string.Empty,
                key.Value?.ToString() ?? throw new MissingKeyException()
            ]);

        return new()
        {
            Cyphertext = result.Ciphertext,
            Iv = result.Iv,
            KeyId = key.Id,
        };
    }
}