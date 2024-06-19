using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Client.Application.Cryptography
{
    public interface ICryptographyService
    {
        Task<Cryptogram> DecryptAsync<T>(Cryptogram cryptogram, Key key) where T : ICryptoHandler;
        Task<Cryptogram> EncryptAsync<T>(Cryptogram cryptogram, Key key) where T : ICryptoHandler;
        Task<Key> GenerateAesKeyAsync(string contact);
    }
}