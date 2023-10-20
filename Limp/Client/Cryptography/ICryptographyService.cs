using Limp.Client.Cryptography.CryptoHandlers;
using LimpShared.Models.Message;

namespace Limp.Client.Cryptography
{
    public interface ICryptographyService
    {
        Task<Cryptogramm> DecryptAsync<T>(Cryptogramm cryptogramm, string? contact = null) where T : ICryptoHandler;
        Task<Cryptogramm> EncryptAsync<T>(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null) where T : ICryptoHandler;
        Task GenerateAESKeyAsync(string contactName, Action<string> callback);
        Task GenerateRSAKeyPairAsync();
    }
}