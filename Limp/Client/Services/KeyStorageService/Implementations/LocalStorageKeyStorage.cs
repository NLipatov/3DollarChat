using System.Text.Json;
using EthachatShared.Encryption;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.KeyStorageService.Implementations;

public class LocalStorageKeyStorage : IKeyStorage
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageKeyStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type)
    {
        var keys = await GetAsync(accessor, type);
        var acceptedKeys = keys.Where(x => x.IsAccepted);
        return acceptedKeys.MaxBy(x => x.CreationDate);
    }

    public async Task StoreAsync(Key key)
    {
        var existingCollection = await GetAsync(key.Contact, key.Type ?? KeyType.Unspecified);

        if (existingCollection.Any(x => x.Id == key.Id))
            return;

        existingCollection.Add(key);

        var serializedKeys = JsonSerializer.Serialize(existingCollection);
        await _jsRuntime.InvokeAsync<string?>("localStorage.setItem", $"key-{key.Contact}-{key.Type}", serializedKeys);
    }

    public async Task DeleteAsync(Key key)
    {
        var storedKeys = await GetAsync(key.Contact, key.Type ?? KeyType.Unspecified);
        var updatedKeys = storedKeys.Where(x => x.CreationDate != key.CreationDate).ToArray();

        var serializedKeys = JsonSerializer.Serialize(updatedKeys);
        await _jsRuntime.InvokeAsync<string?>("localStorage.setItem", $"key-{key.Contact}-{key.Type}", serializedKeys);
    }

    public async Task<List<Key>> GetAsync(string accessor, KeyType type)
    {
        var serializedKeyCollection =
            await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"key-{accessor}-{type}");

        if (serializedKeyCollection is null)
            return [];

        var keyCollection = JsonSerializer.Deserialize<List<Key>>(serializedKeyCollection);

        return keyCollection ?? [];
    }

    public async Task UpdateAsync(Key updatedKey)
    {
        var keys = await GetAsync(updatedKey.Contact, updatedKey.Type ?? KeyType.Unspecified);
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