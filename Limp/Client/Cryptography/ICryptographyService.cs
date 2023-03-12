using Limp.Client.Cryptography.CryptoHandlers;
using Limp.Client.Cryptography.CryptoHandlers.Models;
using LimpShared.Encryption;

namespace Limp.Client.Cryptography
{
    public interface ICryptographyService
    {
        Task<string> DecryptAsync<T>(Cryptogramm cryptogramm, string? contact = null) where T : ICryptoHandler;
        Task<string> EncryptAsync<T>(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null) where T : ICryptoHandler;
        Task GenerateAESKeyAsync(string contactName, Action<string> callback);
        Task GenerateRSAKeyPairAsync();
        void SetAESKey(string contactName, Key key);
    }
}