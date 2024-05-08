using EthachatShared.Encryption;

namespace Ethachat.Client.Services.KeyStorageService;

public interface IKeyStorage
{
    public Task<Key?> GetLastAccepted(string accessor, KeyType type);
    public Task Store(Key key);
    public Task Delete(Key key);
    public Task<List<Key>> Get(string accessor, KeyType type);
}