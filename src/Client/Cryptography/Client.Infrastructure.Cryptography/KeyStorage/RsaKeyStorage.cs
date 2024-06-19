using System.Collections.Concurrent;
using Client.Application.Cryptography.KeyStorage;
using EthachatShared.Encryption;

namespace Client.Infrastructure.Cryptography.KeyStorage;

internal class RsaKeyStorage : IKeyStorage
{
    private static Key? RsaPublic { get; set; }
    private static Key? RsaPrivate { get; set; }
    private static ConcurrentDictionary<string, Key> Storage { get; } = [];

    public Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type)
    {
        if (string.IsNullOrWhiteSpace(accessor))
        {
            if (type is KeyType.RsaPrivate)
                return Task.FromResult(RsaPrivate);
            if (type is KeyType.RsaPublic)
                return Task.FromResult(RsaPublic);
        }

        if (type is KeyType.RsaPublic)
        {
            Storage.TryGetValue(accessor, out var key);
            return Task.FromResult(key);
        }

        throw new ApplicationException($"Unexpected {nameof(Key.Type)} passed in");
    }

    public Task StoreAsync(Key key)
    {
        if (key.Type is KeyType.RsaPrivate)
        {
            if (string.IsNullOrWhiteSpace(key.Contact))
            {
                RsaPrivate = key;
                return Task.CompletedTask;
            }

            throw new ApplicationException($"Unexpected {nameof(Key.Type)} passed in");
        }

        if (key.Type is KeyType.RsaPublic)
        {
            if (string.IsNullOrWhiteSpace(key.Contact))
                RsaPublic = key;
            else
            {
                Storage.AddOrUpdate(key.Contact,
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
            RsaPrivate = null;
        if (key.Type is KeyType.RsaPublic)
            RsaPublic = null;

        return Task.CompletedTask;
    }

    public Task<List<Key>> GetAsync(string accessor, KeyType type)
    {
        if (type is KeyType.RsaPublic)
        {
            if (string.IsNullOrWhiteSpace(accessor))
            {
                return Task.FromResult<List<Key>>(RsaPublic is not null ? [RsaPublic] : []);
            }

            Storage.TryGetValue(accessor, out var partnerKey);
            return Task.FromResult<List<Key>>(partnerKey is not null ? [partnerKey] : []);
        }

        if (type is KeyType.RsaPrivate)
        {
            if (string.IsNullOrWhiteSpace(accessor))
                return Task.FromResult<List<Key>>(RsaPrivate is not null ? [RsaPrivate] : []);
        }

        throw new ApplicationException($"Unexpected {nameof(Key.Type)} passed in");
    }

    public Task UpdateAsync(Key updatedKey)
    {
        if (updatedKey.Type is KeyType.RsaPrivate)
            RsaPrivate = updatedKey;
        if (updatedKey.Type is KeyType.RsaPublic)
            RsaPublic = updatedKey;

        return Task.CompletedTask;
    }
}