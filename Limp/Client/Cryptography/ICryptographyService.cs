using Limp.Client.Cryptography.CryptoHandlers;
using LimpShared.Encryption;

namespace Limp.Client.Cryptography
{
    public interface ICryptographyService
    {
        Task<string> DecryptAsync<T>(string text, string? contact = null) where T : ICryptoHandler;
        Task<string> EncryptAsync<T>(string text, string? contact = null) where T : ICryptoHandler;
        Task GenerateAESKeyAsync(string contactName);
        Task GenerateRSAKeyPairAsync();
        void SetAESKey(string contactName, Key key);
    }
}