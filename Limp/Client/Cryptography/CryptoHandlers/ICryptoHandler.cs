using LimpShared.Encryption;

namespace Limp.Client.Cryptography.CryptoHandlers;

public interface ICryptoHandler
{
    public Task<string> Encrypt(string text, string? contact = null);
    public Task<string> Decrypt(string text, string? contact = null);
}
