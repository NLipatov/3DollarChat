using LimpShared.Encryption;

namespace Limp.Client.Cryptography;

public static class InMemoryKeyStorage
{
    public static Key? RSAPublic { get; set; }
    public static Key? RSAPrivate { get; set; }
    public static Key? AES { get; set; }
    public static Dictionary<string, Key> AESKeyStorage { get; set; } = new();
}
