#nullable disable
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.BrowserKeyStorageService.Models
{
    public class LocalKeyChain
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Dictionary<string, Key> AESKeyStorage { get; set; } = new();
    }
}