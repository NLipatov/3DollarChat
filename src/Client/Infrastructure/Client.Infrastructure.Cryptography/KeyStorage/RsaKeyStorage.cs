using System.Text.Json;
using Client.Application.Cryptography.KeyStorage;
using Client.Application.Runtime;
using EthachatShared.Encryption;

namespace Client.Infrastructure.Cryptography.KeyStorage;

internal class RsaKeyStorage(IPlatformRuntime runtime) : IKeyStorage
{
    private static Key? RsaPublic { get; set; }
    private static Key? RsaPrivate { get; set; }

    public async Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type)
    {
        if (string.IsNullOrWhiteSpace(accessor))
        {
            return type switch
            {
                KeyType.RsaPrivate => RsaPrivate,
                KeyType.RsaPublic => RsaPublic,
                _ => throw new ArgumentException($"Unexpected {nameof(KeyType)}")
            };
        }

        return type switch
        {
            KeyType.RsaPrivate or KeyType.RsaPublic => (await GetAsync(accessor, type)).MaxBy(x => x.CreationDate),
            _ => throw new ArgumentException($"Unexpected {nameof(KeyType)}")
        };
    }

    public async Task StoreAsync(Key key)
    {
        var existingCollection =
            await GetAsync(key.Contact ?? throw new ArgumentException($"Unexpected {nameof(key.Contact)} value"),
                key.Type ?? KeyType.Unspecified);

        if (existingCollection.Any(x => x.Id == key.Id))
            return;

        existingCollection.Add(key);

        var serializedKeys = JsonSerializer.Serialize(existingCollection);
        await runtime.InvokeAsync<string?>("localStorage.setItem", [$"key-{key.Contact}-{key.Type}", serializedKeys]);
    }

    public async Task DeleteAsync(Key key)
    {
        var storedKeys = await GetAsync(key.Contact ?? throw new ArgumentException(), key.Type ?? KeyType.Unspecified);
        var updatedKeys = storedKeys.Where(x => x.CreationDate != key.CreationDate).ToArray();

        var serializedKeys = JsonSerializer.Serialize(updatedKeys);
        await runtime.InvokeAsync<string?>("localStorage.setItem", [$"key-{key.Contact}-{key.Type}", serializedKeys]);
    }

    public async Task<List<Key>> GetAsync(string accessor, KeyType type)
    {
        var serializedKeyCollection =
            await runtime.InvokeAsync<string?>("localStorage.getItem", [$"key-{accessor}-{type}"]);

        if (serializedKeyCollection is null)
            return [];

        var keyCollection = JsonSerializer.Deserialize<List<Key>>(serializedKeyCollection);

        return keyCollection ?? [];
    }

    public async Task UpdateAsync(Key updatedKey)
    {
        var keys = await GetAsync(updatedKey.Contact ?? throw new ArgumentException(),
            updatedKey.Type ?? KeyType.Unspecified);
        var targetKey = keys.FirstOrDefault(x => x.Id == updatedKey.Id);

        if (targetKey is null)
        {
            await StoreAsync(updatedKey);
            return;
        }

        await DeleteAsync(targetKey);
        await StoreAsync(updatedKey);
    }
}