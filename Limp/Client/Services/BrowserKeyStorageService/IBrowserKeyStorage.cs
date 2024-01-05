using Ethachat.Client.Services.BrowserKeyStorageService.Models;
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.BrowserKeyStorageService
{
    public interface IBrowserKeyStorage
    {
        Task<Key?> GetAESKeyForChat(string contactName);
        Task SaveInMemoryKeysInLocalStorage();
        Task<LocalKeyChain?> ReadLocalKeyChainAsync();
        Task<bool> IsAESKeyReady(string contactName);
    }
}
