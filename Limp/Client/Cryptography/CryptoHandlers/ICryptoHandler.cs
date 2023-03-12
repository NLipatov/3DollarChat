using Limp.Client.Cryptography.CryptoHandlers.Models;
using LimpShared.Encryption;

namespace Limp.Client.Cryptography.CryptoHandlers;

public interface ICryptoHandler
{
    public Task<Cryptogramm> Encrypt(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null);
    public Task<Cryptogramm> Decrypt(Cryptogramm cryptogramm, string? contact = null);
}
