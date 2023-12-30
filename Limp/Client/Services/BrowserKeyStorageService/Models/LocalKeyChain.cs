#nullable disable
using LimpShared.Encryption;

namespace Ethachat.Client.Services.CloudKeyService.Models
{
    public class LocalKeyChain
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Dictionary<string, Key> AESKeyStorage { get; set; } = new();
    }
}