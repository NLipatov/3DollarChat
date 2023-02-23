using LimpShared.Encryption;

namespace Limp.Client.Cryptography.CryptoHandlers;

public interface ICryptoHandler
{
    public Task<string> Encrypt(string text);
    public Task<string> Decrypt(string text);
}
