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
        public string localStorageKeychainObjectName { get; set; } = "localKeyChain";
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

        public async Task<bool> IsAESKeyReady(string contactName)
        {
            LocalKeyChain? keyChain = await ReadLocalKeyChainAsync();
            string? targetKey = keyChain?.AESKeyStorage.Keys.FirstOrDefault(x => x == contactName);

            return targetKey != null;
        }

        public async Task<Key?> GetAESKeyForChat(string contactName)
        {
            LocalKeyChain? localKeyChain = await ReadLocalKeyChainAsync();
            Key? key = localKeyChain?.AESKeyStorage.FirstOrDefault(x => x.Key == contactName).Value;

            Key? keyFromInMemoryService = InMemoryKeyStorage.AESKeyStorage.FirstOrDefault(x => x.Key == contactName).Value;

            if (keyFromInMemoryService == null && key != null)
            {
                Console.WriteLine("Key was not found in memory, using key from local storage.");
                InMemoryKeyStorage.AESKeyStorage.Add(contactName, key);
                keyFromInMemoryService = InMemoryKeyStorage.AESKeyStorage.FirstOrDefault(x => x.Key == contactName).Value;
            }
            else if (keyFromInMemoryService != null)
            {
                Console.WriteLine($"Using key created at: {keyFromInMemoryService.CreationDate.ToLocalTime().ToString("dd/MM HH:mm")}");
            }
            else
            {
                Console.WriteLine("No key found");
            }

            return keyFromInMemoryService;
        }
    }
}
