using EthachatShared.Models.Message;

namespace Ethachat.Client.Cryptography.CryptoHandlers;

public interface ICryptoHandler
{
    public Task<Cryptogram> Encrypt(Cryptogram cryptogram, string? contact = null, string? PublicKeyToEncryptWith = null);
    public Task<Cryptogram> Decrypt(Cryptogram cryptogram, string? contact = null);
}
