using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;

namespace Client.Application.Cryptography
{
    public interface ICryptographyService
    {
        public Task<BinaryCryptogram> DecryptAsync<T>(BinaryCryptogram cryptogram, Key key)
            where T : ICryptoHandler;

        public Task<BinaryCryptogram> EncryptAsync<TCryptoHandler, TData>(TData data, Key key) where TCryptoHandler : ICryptoHandler;
        Task<Key> GenerateAesKeyAsync(string contact, string author);
    }
}