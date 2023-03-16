using LimpShared.Encryption;
using System.Collections.Concurrent;

namespace Limp.Client.Cryptography.KeyStorage;

public static class InMemoryKeyStorage
{
    public static bool isPublicKeySet { get; set; } = false;
    public static Key? MyRSAPublic { get; set; }
    public static Key? MyRSAPrivate { get; set; }
    public static ConcurrentDictionary<string, Key> AESKeyStorage { get; set; } = new();
    public static ConcurrentDictionary<string, Key> RSAKeyStorage { get; set; } = new();
}
