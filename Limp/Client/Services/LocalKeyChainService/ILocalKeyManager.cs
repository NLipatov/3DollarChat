using Limp.Client.Services.CloudKeyService.Models;
using LimpShared.Encryption;

namespace Limp.Client.Services.CloudKeyService
{
    public interface ILocalKeyManager
    {
        Task<Key?> GetAESKeyForChat(string contactName);
        Task SynchronizeWithInMemoryKeyStorageAsync();
        Task SaveLocalKeyChainAsync(LocalKeyChain LocalKeyToStore);
        Task<LocalKeyChain?> ReadLocalKeyChainAsync();
        Task<bool> IsAESKeyReady(string contactName);
    }
}
