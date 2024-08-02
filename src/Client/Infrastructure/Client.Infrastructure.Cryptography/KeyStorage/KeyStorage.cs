using Client.Application.Cryptography.KeyStorage;
using Client.Application.Runtime;
using EthachatShared.Encryption;

namespace Client.Infrastructure.Cryptography.KeyStorage;

public class KeyStorage(IPlatformRuntime runtime) : IKeyStorage
{
    private readonly AesKeyStorage _aesKeyStorage = new(runtime);
    private readonly RsaKeyStorage _rsaKeyStorage = new(runtime);

    public async Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type)
    {
        if (type is KeyType.RsaPublic || type is KeyType.RsaPrivate)
            return await _rsaKeyStorage.GetLastAcceptedAsync(accessor, type);
        return await _aesKeyStorage.GetLastAcceptedAsync(accessor, type);
    }

    public async Task StoreAsync(Key key)
    {
        var storeTask = key.Type switch
        {
            KeyType.RsaPublic or KeyType.RsaPrivate => _rsaKeyStorage.StoreAsync(key),
            KeyType.Aes => _aesKeyStorage.StoreAsync(key),
            _ => Task.FromException(new ArgumentException($"Unexpected {nameof(key.Type)}"))
        };

        await storeTask;
    }

    public async Task DeleteAsync(Key key)
    {
        if (key.Type is KeyType.RsaPublic || key.Type is KeyType.RsaPrivate)
            await _rsaKeyStorage.DeleteAsync(key);
        await _aesKeyStorage.DeleteAsync(key);
    }

    public async Task<List<Key>> GetAsync(string accessor, KeyType type)
    {
        return type switch
        {
            KeyType.RsaPublic or KeyType.RsaPrivate => await _rsaKeyStorage.GetAsync(accessor, type),
            _ => await _aesKeyStorage.GetAsync(accessor, type)
        };
    }

    public async Task UpdateAsync(Key updatedKey)
    {
        if (updatedKey.Type is KeyType.RsaPublic || updatedKey.Type is KeyType.RsaPrivate)
            await _rsaKeyStorage.UpdateAsync(updatedKey);
        await _aesKeyStorage.UpdateAsync(updatedKey);
    }
}