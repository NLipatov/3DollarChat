using Limp.Client.Cryptography.CryptoHandlers.Models;
using LimpShared.Encryption;

namespace Limp.Client.Cryptography.CryptoHandlers;

public interface ICryptoHandler
{
    public Task<string> Encrypt(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null);
    public Task<string> Decrypt(Cryptogramm cryptogramm, string? contact = null);
}
