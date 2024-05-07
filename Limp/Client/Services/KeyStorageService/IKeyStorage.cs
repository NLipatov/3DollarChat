using EthachatShared.Encryption;

namespace Ethachat.Client.Services.KeyStorageService;

public interface IKeyStorage
{
    public Task Store(Key key);
    public Task<Key?> Get(string accessor, KeyType type);
}