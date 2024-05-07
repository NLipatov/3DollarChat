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

    public async Task Store(Key key)
    {
        var serializedKey = JsonSerializer.Serialize(key);
        await _jsRuntime.InvokeAsync<string?>("localStorage.setItem", $"aes-key-{key.Contact}-{key.Type}");
    }

    public async Task<Key?> Get(string accessor, KeyType type)
    {
        string? serializedKey =
            await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"aes-key-{accessor}-{type}");
        var key = JsonSerializer.Deserialize<Key>(serializedKey);

        return key;
    }
}