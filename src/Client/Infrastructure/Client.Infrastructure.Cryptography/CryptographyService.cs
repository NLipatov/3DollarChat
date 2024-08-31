using Client.Application.Cryptography;
using Client.Application.Runtime;
using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;

namespace Client.Infrastructure.Cryptography;

public class CryptographyService(IPlatformRuntime cryptographyExecutor) : ICryptographyService
{
    private readonly CryptoTaskQueue _cryptoTaskQueue = new();

    public Task<BinaryCryptogram> DecryptAsync<T>(BinaryCryptogram cryptogram, Key key)
        where T : ICryptoHandler
    {
        var taskCompletionSource = new TaskCompletionSource<BinaryCryptogram>();

        _cryptoTaskQueue.EnqueueTask(async () =>
        {
            try
            {
                var handler = (T?)Activator.CreateInstance(typeof(T), cryptographyExecutor);
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

    public Task<BinaryCryptogram> EncryptAsync<TCryptoHandler, TData>(TData data, Key key)
        where TCryptoHandler : ICryptoHandler
    {
        var taskCompletionSource = new TaskCompletionSource<BinaryCryptogram>();

        _cryptoTaskQueue.EnqueueTask(async () =>
        {
            try
            {
                var handler = (TCryptoHandler?)Activator.CreateInstance(typeof(TCryptoHandler), cryptographyExecutor);
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