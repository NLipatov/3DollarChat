using System.Collections.Concurrent;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using EthachatShared.Encryption;

namespace Client.Infrastructure.Cryptography.KeyStorage;

public class RsaKeyStorage(IPlatformRuntime runtime) : IKeyStorage<RsaHandler>
{
    private static Key? MyRsaPublic { get; set; }
    private static Key? MyRsaPrivate { get; set; }
    private static ConcurrentDictionary<string, Key> RSAKeyStorage { get; set; } = new();
    public Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type)
    {
        if (string.IsNullOrWhiteSpace(accessor))
        {
            if (type is KeyType.RsaPrivate)
                return Task.FromResult(MyRsaPrivate);
            if (type is KeyType.RsaPublic)
                return Task.FromResult(MyRsaPublic);
        }

        if (type is KeyType.RsaPublic)
        {
            RSAKeyStorage.TryGetValue(accessor, out var key);
            return Task.FromResult(key);
        }

        throw new ApplicationException($"Unexpected {nameof(Key.Type)} passed in");
    }

    public Task StoreAsync(Key key)
    {
        if (key.Type is KeyType.RsaPrivate)
        {
            if (string.IsNullOrWhiteSpace(key.Contact))
                MyRsaPrivate = key;
            throw new ApplicationException($"Unexpected {nameof(Key.Type)} passed in");
        }

        if (key.Type is KeyType.RsaPublic)
        {
            if (string.IsNullOrWhiteSpace(key.Contact))
                MyRsaPublic = key;
            else
            {
                RSAKeyStorage.AddOrUpdate(key.Contact,
                    _ => key,
                    (_, existingKey) =>
                    {
                        existingKey = key;
                        return existingKey;
                    });
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Key key)
    {
        if (key.Type is KeyType.RsaPrivate)
            MyRsaPrivate = null;
        if (key.Type is KeyType.RsaPublic)
            MyRsaPublic = null;

        return Task.CompletedTask;
    }

    public Task<List<Key>> GetAsync(string accessor, KeyType type)
    {
        return Task.FromResult<List<Key>>(MyRsaPublic is not null ? [MyRsaPublic] : []);
    }

    public Task UpdateAsync(Key updatedKey)
    {
        if (updatedKey.Type is KeyType.RsaPrivate)
            MyRsaPrivate = updatedKey;
        if (updatedKey.Type is KeyType.RsaPublic)
            MyRsaPublic = updatedKey;
        
        return Task.CompletedTask;
    }
}