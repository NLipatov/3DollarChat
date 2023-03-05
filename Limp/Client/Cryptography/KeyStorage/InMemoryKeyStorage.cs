using LimpShared.Encryption;

namespace Limp.Client.Cryptography.KeyStorage;

public static class InMemoryKeyStorage
{
    public static bool isPublicKeySet { get; set; } = false;
    public static Key? MyRSAPublic { get; set; }
    public static Key? MyRSAPrivate { get; set; }
    public static Dictionary<string, Key> AESKeyStorage { get; set; } = new();
    public static Dictionary<string, Key> RSAKeyStorage { get; set;} = new();
}
