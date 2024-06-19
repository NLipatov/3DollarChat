using Client.Application.Cryptography.KeyStorage;
using EthachatShared.Encryption;

namespace Client.Infrastructure.Cryptography.KeyStorage;

public class KeyStorage(IPlatformRuntime runtime) : IKeyStorage
{
    private readonly AesKeyStorage _aesKeyStorage = new(runtime);
    private readonly RsaKeyStorage _rsaKeyStorage = new();

    public async Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type)
    {
        if (type is KeyType.RsaPublic || type is KeyType.RsaPrivate)
            return await _rsaKeyStorage.GetLastAcceptedAsync(accessor, type);
        return await _aesKeyStorage.GetLastAcceptedAsync(accessor, type);
    }

    public async Task StoreAsync(Key key)
    {
        if (key.Type is KeyType.RsaPublic || key.Type is KeyType.RsaPrivate)
            await _rsaKeyStorage.StoreAsync(key);
        await _aesKeyStorage.StoreAsync(key);
    }

    public async Task DeleteAsync(Key key)
    {
        if (key.Type is KeyType.RsaPublic || key.Type is KeyType.RsaPrivate)
            await _rsaKeyStorage.DeleteAsync(key);
        await _aesKeyStorage.DeleteAsync(key);
    }

    public async Task<List<Key>> GetAsync(string accessor, KeyType type)
    {
        if (type is KeyType.RsaPublic || type is KeyType.RsaPrivate)
            return await _rsaKeyStorage.GetAsync(accessor, type);
        return await _aesKeyStorage.GetAsync(accessor, type);
    }

    public async Task UpdateAsync(Key updatedKey)
    {
        if (updatedKey.Type is KeyType.RsaPublic || updatedKey.Type is KeyType.RsaPrivate)
            await _rsaKeyStorage.UpdateAsync(updatedKey);
        await _aesKeyStorage.UpdateAsync(updatedKey);
    }
}