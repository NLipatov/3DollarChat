using EthachatShared.Encryption;

namespace Ethachat.Client.Services.KeyStorageService.Implementations;

public class InMemoryKeyStorage : IKeyStorage
{
    public async Task Store(Key key)
    {
        Cryptography.KeyStorage.InMemoryKeyStorage.AESKeyStorage.TryAdd(key.Contact, key);
    }

    public async Task<Key?> Get(string accessor, KeyType type)
    {
        Cryptography.KeyStorage.InMemoryKeyStorage.AESKeyStorage.TryGetValue(accessor, out var key);
        return key;
    }
}