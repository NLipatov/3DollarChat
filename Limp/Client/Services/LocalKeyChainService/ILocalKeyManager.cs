using Limp.Client.Services.CloudKeyService.Models;
using LimpShared.Encryption;

namespace Limp.Client.Services.CloudKeyService
{
    public interface ILocalKeyManager
    {
        Task SynchronizeWithInMemoryKeyStorageAsync();
        Task SaveLocalKeyChainAsync(LocalKeyChain LocalKeyToStore);
        Task<LocalKeyChain?> ReadLocalKeyChainAsync();
    }
}
