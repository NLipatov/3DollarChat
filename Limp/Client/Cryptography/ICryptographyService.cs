using Ethachat.Client.Cryptography.CryptoHandlers;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Cryptography
{
    public interface ICryptographyService
    {
        Task<Cryptogramm> DecryptAsync<T>(Cryptogramm cryptogram, string? contact = null) where T : ICryptoHandler;
        Task<Cryptogramm> EncryptAsync<T>(Cryptogramm cryptogram, string? contact = null, string? publicKeyToEncryptWith = null) where T : ICryptoHandler;
        Task GenerateAesKeyAsync(string contactName, Action<string> callback);
        Task GenerateRsaKeyPairAsync();
    }
}