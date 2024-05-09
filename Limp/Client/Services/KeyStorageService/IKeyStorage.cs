using EthachatShared.Encryption;

namespace Ethachat.Client.Services.KeyStorageService;

public interface IKeyStorage
{
    public Task<Key?> GetLastAcceptedAsync(string accessor, KeyType type);
    public Task StoreAsync(Key key);
    public Task DeleteAsync(Key key);
    public Task<List<Key>> GetAsync(string accessor, KeyType type);
    public Task UpdateAsync(Key updatedKey);
}