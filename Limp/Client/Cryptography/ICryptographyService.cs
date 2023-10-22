using Limp.Client.Cryptography.CryptoHandlers;
using LimpShared.Models.Message;

namespace Limp.Client.Cryptography
{
    public interface ICryptographyService
    {
        Task<Cryptogramm> DecryptAsync<T>(Cryptogramm cryptogram, string? contact = null) where T : ICryptoHandler;
        Task<Cryptogramm> EncryptAsync<T>(Cryptogramm cryptogram, string? contact = null, string? publicKeyToEncryptWith = null) where T : ICryptoHandler;
        Task GenerateAesKeyAsync(string contactName, Action<string> callback);
        Task GenerateRsaKeyPairAsync();
    }
}