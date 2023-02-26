using LimpShared.Encryption;

namespace Limp.Client.Cryptography.KeyStorage;

public static class InMemoryKeyStorage
{
    public static bool isPublicKeySet { get; set; } = false;
    public static Key? RSAPublic { get; set; }
    public static Key? RSAPrivate { get; set; }
    public static Dictionary<string, Key> AESKeyStorage { get; set; } = new();
}
