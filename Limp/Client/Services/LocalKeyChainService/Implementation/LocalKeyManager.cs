using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.Services.CloudKeyService;
using Limp.Client.Services.CloudKeyService.Models;
using LimpShared.Encryption;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Limp.Client.Services.LocalKeyChainService.Implementation
{
    public class LocalKeyManager : ILocalKeyManager
    {
        public string localStorageKeychainObjectName { get; set; } = "lkc";
        public string Password { get; set; } = string.Empty;
        private readonly IJSRuntime _jSRuntime;

        public LocalKeyManager(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task SynchronizeWithInMemoryKeyStorageAsync()
        {
            Dictionary<string, Key> inMemoryStoredKeys = InMemoryKeyStorage.AESKeyStorage;
            LocalKeyChain localKeyChain = new()
            {
                Name = localStorageKeychainObjectName,
                AESKeyStorage = inMemoryStoredKeys
            };

            await SaveLocalKeyChainAsync(localKeyChain);
        }

        public async Task<LocalKeyChain?> ReadLocalKeyChainAsync()
        {
            string? encryptedKeyChain = await _jSRuntime.InvokeAsync<string?>("localStorage.getItem", localStorageKeychainObjectName);
            if(string.IsNullOrWhiteSpace(encryptedKeyChain))
                return null;

            LocalKeyChain? localKeyChain = JsonSerializer.Deserialize<LocalKeyChain>(encryptedKeyChain);
            return localKeyChain;
        }

        public async Task SaveLocalKeyChainAsync(LocalKeyChain LocalKeyToStore)
        {
            string serializedPlainLocalKeyChain = JsonSerializer.Serialize(LocalKeyToStore);
            await _jSRuntime.InvokeVoidAsync("localStorage.setItem", LocalKeyToStore.Name, serializedPlainLocalKeyChain);
        }
    }
}
