using Client.Application.Cryptography;
using Client.Application.Runtime;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;

namespace Client.Infrastructure.Cryptography;

public class CryptographyService(IPlatformRuntime cryptographyExecutor) : ICryptographyService
{
    private readonly CryptoTaskQueue _cryptoTaskQueue = new();

    public async Task<BinaryCryptogram> DecryptAsync<T>(BinaryCryptogram cryptogram, Key key)
        where T : ICryptoHandler
    {

        var handler = (T?)Activator.CreateInstance(typeof(T), cryptographyExecutor);
        if (handler is null)
            throw new NullReferenceException();

        return await handler.Decrypt(cryptogram, key);
    }

    public async Task<BinaryCryptogram> EncryptAsync<TCryptoHandler, TData>(TData data, Key key)
        where TCryptoHandler : ICryptoHandler
    {
        var handler = (TCryptoHandler?)Activator.CreateInstance(typeof(TCryptoHandler), cryptographyExecutor);
        if (handler is null)
            throw new NullReferenceException();

        return await handler.Encrypt(data, key);
    }

    public async Task<Key> GenerateAesKeyAsync(string contact, string author)
    {
        var key = await cryptographyExecutor.InvokeAsync<string>("GenerateAESKeyAsync", []);
        return new Key
        {
            Id = Guid.NewGuid(),
            Value = key,
            Format = KeyFormat.Raw,
            Type = KeyType.Aes,
            Contact = contact,
            Author = author,
            CreationDate = DateTime.UtcNow,
            IsAccepted = false
        };
    }
}
