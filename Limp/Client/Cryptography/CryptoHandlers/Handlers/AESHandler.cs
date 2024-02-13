using Ethachat.Client.Cryptography.KeyStorage;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography.CryptoHandlers.Handlers
{
    public class AESHandler : ICryptoHandler
    {
        private readonly IJSRuntime _jSRuntime;

        public AESHandler(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<Cryptogramm> Decrypt(Cryptogramm cryptogramm, string? contact = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cryptogramm.Iv))
                    throw new ArgumentException("Please provide an IV");

                await _jSRuntime.InvokeVoidAsync("ImportIV", cryptogramm.Iv, cryptogramm.Cyphertext);

                Key? key = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact);
                if (key == null)
                    throw new ApplicationException("RSA Public key was null");

                await _jSRuntime.InvokeVoidAsync("importSecretKey", key.Value.ToString());

                string decryptedMessage = string.Empty;
                if (!string.IsNullOrWhiteSpace(cryptogramm.Cyphertext))
                {
                    decryptedMessage = await _jSRuntime
                        .InvokeAsync<string>("AESDecryptText", cryptogramm.Cyphertext, key.Value.ToString());
                }

                var result = new Cryptogramm()
                {
                    Cyphertext = decryptedMessage,
                    Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogramm.Cyphertext)
                };

                await _jSRuntime.InvokeVoidAsync("DeleteIv", cryptogramm.Cyphertext);

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message, ex);
            }
        }

        public async Task<Cryptogramm> Encrypt(Cryptogramm cryptogramm, string? contact = null, string? PublicKeyToEncryptWith = null)
        {
            try
            {
                string? aesKey = string.Empty;

                if (!string.IsNullOrWhiteSpace(PublicKeyToEncryptWith))
                    aesKey = PublicKeyToEncryptWith;
                else if (!string.IsNullOrWhiteSpace(contact))
                    aesKey = InMemoryKeyStorage.AESKeyStorage.GetValueOrDefault(contact)?.Value?.ToString();

                if (string.IsNullOrWhiteSpace(aesKey))
                    throw new ApplicationException("Could not resolve a AES key for encryption.");

                string encryptedText = string.Empty;
                if (!string.IsNullOrWhiteSpace(cryptogramm.Cyphertext))
                {
                    encryptedText = await _jSRuntime
                        .InvokeAsync<string>("AESEncryptText", cryptogramm.Cyphertext, aesKey);
                }

                var result = new Cryptogramm
                {
                    Cyphertext = encryptedText,
                    Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogramm.Cyphertext),
                };
                
                await _jSRuntime.InvokeVoidAsync("DeleteIv", cryptogramm.Cyphertext);

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message, ex);
            }
        }
    }
}
