using EthachatShared.Models.Message;

namespace Client.Application.Cryptography;

public interface ICryptoHandler
{
    public Task<Cryptogram> Encrypt(Cryptogram cryptogram, string? contact = null, string? PublicKeyToEncryptWith = null);
    public Task<Cryptogram> Decrypt(Cryptogram cryptogram, string? contact = null);
}
