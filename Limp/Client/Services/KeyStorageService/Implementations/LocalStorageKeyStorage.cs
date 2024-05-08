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

    public async Task<Key?> GetLastAccepted(string accessor, KeyType type)
    {
        var keys = await Get(accessor, type);
        var acceptedKeys = keys.Where(x => x.IsAccepted);
        return acceptedKeys.MaxBy(x => x.CreationDate);
    }

    public async Task Store(Key key)
    {
        var existingCollection = await Get(key.Contact, key.Type ?? KeyType.Unspecified);
        existingCollection.Add(key);

        var serializedKeys = JsonSerializer.Serialize(existingCollection);
        await _jsRuntime.InvokeAsync<string?>("localStorage.setItem", $"key-{key.Contact}-{key.Type}", serializedKeys);
    }

    public async Task Delete(Key key)
    {
        var storedKeys = await Get(key.Contact, key.Type ?? KeyType.Unspecified);
        var updatedKeys = storedKeys.Where(x => x.CreationDate != key.CreationDate).ToArray();
        
        var serializedKeys = JsonSerializer.Serialize(updatedKeys);
        await _jsRuntime.InvokeAsync<string?>("localStorage.setItem", $"key-{key.Contact}-{key.Type}", serializedKeys);
    }

    public async Task<List<Key>> Get(string accessor, KeyType type)
    {
        var serializedKeyCollection =
            await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"key-{accessor}-{type}");
        
        if (serializedKeyCollection is null)
            return [];

        var keyCollection = JsonSerializer.Deserialize<List<Key>>(serializedKeyCollection);

        return keyCollection ?? [];
    }
}