using Ethachat.Client.Services.CloudKeyService.Models;
using LimpShared.Encryption;

namespace Ethachat.Client.Services.CloudKeyService
{
    public interface IBrowserKeyStorage
    {
        Task<Key?> GetAESKeyForChat(string contactName);
        Task SaveInMemoryKeysInLocalStorage();
        Task<LocalKeyChain?> ReadLocalKeyChainAsync();
        Task<bool> IsAESKeyReady(string contactName);
    }
}
