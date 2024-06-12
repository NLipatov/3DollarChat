using System.Collections.Concurrent;
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.KeyStorageService.KeyStorage;

public static class InMemoryKeyStorage
{
    public static Key? MyRSAPublic { get; set; }
    public static Key? MyRSAPrivate { get; set; }
    public static ConcurrentDictionary<string, Key> RSAKeyStorage { get; set; } = new();
}
