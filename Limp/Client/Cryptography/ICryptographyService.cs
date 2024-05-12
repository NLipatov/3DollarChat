using Ethachat.Client.Cryptography.CryptoHandlers;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Cryptography
{
    public interface ICryptographyService
    {
        Task<Cryptogram> DecryptAsync<T>(Cryptogram cryptogram, string? contact = null) where T : ICryptoHandler;
        Task<Cryptogram> EncryptAsync<T>(Cryptogram cryptogram, string? contact = null, string? publicKeyToEncryptWith = null) where T : ICryptoHandler;
        Task<Key> GenerateAesKeyAsync(string contact);
        Task GenerateRsaKeyPairAsync();
    }
}