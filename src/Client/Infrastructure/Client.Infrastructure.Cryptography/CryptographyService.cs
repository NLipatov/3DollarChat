using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Application.Runtime;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;

namespace Client.Infrastructure.Cryptography;

public class CryptographyService : ICryptographyService
{
    private readonly IPlatformRuntime _platformRuntime;
    private readonly IKeyStorage _keyStorage;
    private readonly CryptoTaskQueue _cryptoTaskQueue = new();

    public CryptographyService(IPlatformRuntime platformRuntime, IKeyStorage keyStorage)
    {
        _platformRuntime = platformRuntime;
        _keyStorage = keyStorage;
        _ = GenerateRsaKeyPairAsync();
    }

    public Task<BinaryCryptogram> DecryptAsync<T>(BinaryCryptogram cryptogram, Key key)
        where T : ICryptoHandler
    {
        var taskCompletionSource = new TaskCompletionSource<BinaryCryptogram>();

        _cryptoTaskQueue.EnqueueTask(async () =>
        {
            try
            {
                var handler = (T?)Activator.CreateInstance(typeof(T), _platformRuntime);
                if (handler is null)
                    throw new NullReferenceException();

                var result = await handler.Decrypt(cryptogram, key);
                taskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        });

        return taskCompletionSource.Task;
    }


    public Task<BinaryCryptogram> EncryptAsync<TCryptoHandler>(byte[] data, Key key)
        where TCryptoHandler : ICryptoHandler
    {
        var taskCompletionSource = new TaskCompletionSource<BinaryCryptogram>();

        _cryptoTaskQueue.EnqueueTask(async () =>
        {
            try
            {
                var handler = (TCryptoHandler?)Activator.CreateInstance(typeof(TCryptoHandler), _platformRuntime);
                if (handler is null)
                    throw new NullReferenceException();

                var result = await handler.Encrypt(data, key);
                taskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        });

        return taskCompletionSource.Task;
    }

    public async Task<Key> GenerateAesKeyAsync(string contact, string author)
    {
        var key = await _platformRuntime.InvokeAsync<string>("GenerateAESKeyAsync", []);
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

    private async Task GenerateRsaKeyPairAsync()
    {
        var keyPair = await _platformRuntime.InvokeAsync<string[]>("GenerateRSAOAEPKeyPairAsync", []);
        var publicRsa = new Key
        {
            Id = Guid.NewGuid(),
            Value = keyPair[0],
            Format = KeyFormat.PemSpki,
            Type = KeyType.RsaPublic,
            Contact = string.Empty
        };
        var privateRsa = new Key
        {
            Id = Guid.NewGuid(),
            Value = keyPair[1],
            Format = KeyFormat.PemSpki,
            Type = KeyType.RsaPrivate,
            Contact = string.Empty
        };
        await _keyStorage.StoreAsync(privateRsa);
        await _keyStorage.StoreAsync(publicRsa);
    }
}