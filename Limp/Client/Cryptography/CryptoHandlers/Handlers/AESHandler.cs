using System.Security.Cryptography;
using Ethachat.Client.Services.KeyStorageService.Implementations;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;
using InMemoryKeyStorage = Ethachat.Client.Cryptography.KeyStorage.InMemoryKeyStorage;

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
                var localKeyStorageService = new LocalStorageKeyStorage(_jSRuntime);
                
                if (string.IsNullOrWhiteSpace(cryptogramm.Iv))
                    throw new ArgumentException("Please provide an IV");

                await _jSRuntime.InvokeVoidAsync("ImportIV", cryptogramm.Iv, cryptogramm.Cyphertext);

                var keys = await localKeyStorageService.Get(contact, KeyType.Aes);
                if (!keys.Any())
                    throw new ApplicationException($"No keys stored for {contact}");
                var key = keys.FirstOrDefault(x => x.CreationDate.Date == cryptogramm.KeyDateTime.Date 
                                                   && x.CreationDate.Hour == cryptogramm.KeyDateTime.Hour 
                                                   &&  x.CreationDate.Minute == cryptogramm.KeyDateTime.Minute
                                                   &&  x.CreationDate.Second == cryptogramm.KeyDateTime.Second);

                if (key is null)
                    throw new ApplicationException("Message was encrypted with key that is not presented in key storage.");
                
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
                var localKeyStorageService = new LocalStorageKeyStorage(_jSRuntime);
                Key key = await localKeyStorageService.GetLastAccepted(contact, KeyType.Aes);

                if (key is null)
                    throw new ApplicationException("Could not resolve a AES key for encryption.");

                string encryptedText = string.Empty;
                if (!string.IsNullOrWhiteSpace(cryptogramm.Cyphertext))
                {
                    encryptedText = await _jSRuntime
                        .InvokeAsync<string>("AESEncryptText", cryptogramm.Cyphertext, key.Value!.ToString());
                }

                var result = new Cryptogramm
                {
                    Cyphertext = encryptedText,
                    Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogramm.Cyphertext),
                    KeyDateTime = key.CreationDate,
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
