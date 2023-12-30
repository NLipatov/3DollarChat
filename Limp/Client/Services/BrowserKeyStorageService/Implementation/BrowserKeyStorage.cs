using EthachatShared.Encryption;
using Microsoft.JSInterop;
using System.Text.Json;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.Services.CloudKeyService;
using Ethachat.Client.Services.CloudKeyService.Models;

namespace Ethachat.Client.Services.LocalKeyChainService.Implementation
{
    public class BrowserKeyStorage : IBrowserKeyStorage
    {
        public string localStorageKeyChainObjectName { get; set; } = "localKeyChain";
        public string Password { get; set; } = string.Empty;
        private readonly IJSRuntime _jSRuntime;

        public BrowserKeyStorage(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task SaveInMemoryKeysInLocalStorage()
        {
            Dictionary<string, Key> inMemoryStoredKeys = InMemoryKeyStorage.AESKeyStorage;
            LocalKeyChain localKeyChain = new()
            {
                Name = localStorageKeyChainObjectName,
                AESKeyStorage = inMemoryStoredKeys
            };

            await SaveLocalKeyChainAsync(localKeyChain);
        }

        public async Task<LocalKeyChain?> ReadLocalKeyChainAsync()
        {
            string? encryptedKeyChain = await _jSRuntime.InvokeAsync<string?>("localStorage.getItem", localStorageKeyChainObjectName);
            if(string.IsNullOrWhiteSpace(encryptedKeyChain))
                return null;

            LocalKeyChain? localKeyChain = JsonSerializer.Deserialize<LocalKeyChain>(encryptedKeyChain);
            return localKeyChain;
        }

        private async Task SaveLocalKeyChainAsync(LocalKeyChain LocalKeyToStore)
        {
            string serializedPlainLocalKeyChain = JsonSerializer.Serialize(LocalKeyToStore);
            await _jSRuntime.InvokeVoidAsync("localStorage.setItem", LocalKeyToStore.Name, serializedPlainLocalKeyChain);
        }

        public async Task<bool> IsAESKeyReady(string contactName)
        {
            LocalKeyChain? keyChain = await ReadLocalKeyChainAsync();
            string? targetKey = keyChain?.AESKeyStorage.Keys.FirstOrDefault(x => x == contactName);

            return targetKey != null;
        }

        public async Task<Key?> GetAESKeyForChat(string contactName)
        {
            LocalKeyChain? localKeyChain = await ReadLocalKeyChainAsync();
            Key? browserkey = localKeyChain?.AESKeyStorage.FirstOrDefault(x => x.Key == contactName).Value;

            Key? inMemoryKey = InMemoryKeyStorage.AESKeyStorage.FirstOrDefault(x => x.Key == contactName).Value;

            if (inMemoryKey == null && browserkey != null)
            {
                InMemoryKeyStorage.AESKeyStorage.Add(contactName, browserkey);
                inMemoryKey = InMemoryKeyStorage.AESKeyStorage.FirstOrDefault(x => x.Key == contactName).Value;
            }
            else if(inMemoryKey?.Value != null && inMemoryKey.Value.ToString() != browserkey?.Value?.ToString())
            {
                //Saving InMemoryKey in browser's local storage
                await SaveInMemoryKeysInLocalStorage();
            }

            return inMemoryKey;
        }
    }
}
