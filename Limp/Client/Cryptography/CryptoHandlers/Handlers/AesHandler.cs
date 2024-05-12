using Ethachat.Client.Services.KeyStorageService.Implementations;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Cryptography.CryptoHandlers.Handlers
{
    public class AesHandler : ICryptoHandler
    {
        private readonly IJSRuntime _jSRuntime;

        public AesHandler(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }
        public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, string? contact = null)
        {
            try
            {
                var localKeyStorageService = new LocalStorageKeyStorage(_jSRuntime);
                
                if (string.IsNullOrWhiteSpace(cryptogram.Iv))
                    throw new ArgumentException("Please provide an IV");

                await _jSRuntime.InvokeVoidAsync("ImportIV", cryptogram.Iv, cryptogram.Cyphertext);

                var keys = await localKeyStorageService.GetAsync(contact, KeyType.Aes);
                if (!keys.Any())
                    throw new ApplicationException($"No keys stored for {contact}");
                
                var key = keys.FirstOrDefault(x => x.Id == cryptogram.KeyId);

                //Contact has some previous key, but not the latest one.
                //In this case we need to mark that last key that contact has as last accepted key 
                var supposedKey = await localKeyStorageService.GetLastAcceptedAsync(contact, KeyType.Aes);
                if (key is not null && supposedKey is not null && key.Id != supposedKey.Id)
                {
                    key.CreationDate = DateTime.UtcNow;
                    await localKeyStorageService.UpdateAsync(key);
                }

                if (key is null)
                    throw new ApplicationException("Message was encrypted with key that is not presented in key storage.");
                
                await _jSRuntime.InvokeVoidAsync("importSecretKey", key.Value.ToString());

                string decryptedMessage = string.Empty;
                if (!string.IsNullOrWhiteSpace(cryptogram.Cyphertext))
                {
                    decryptedMessage = await _jSRuntime
                        .InvokeAsync<string>("AESDecryptText", cryptogram.Cyphertext, key.Value.ToString());
                }

                var result = new Cryptogram()
                {
                    Cyphertext = decryptedMessage,
                    Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogram.Cyphertext)
                };

                await _jSRuntime.InvokeVoidAsync("DeleteIv", cryptogram.Cyphertext);

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message, ex);
            }
        }

        public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, string? contact = null, string? PublicKeyToEncryptWith = null)
        {
            try
            {
                var localKeyStorageService = new LocalStorageKeyStorage(_jSRuntime);
                Key key = await localKeyStorageService.GetLastAcceptedAsync(contact, KeyType.Aes);

                if (key is null)
                    throw new ApplicationException("Could not resolve a AES key for encryption.");

                string encryptedText = string.Empty;
                if (!string.IsNullOrWhiteSpace(cryptogram.Cyphertext))
                {
                    encryptedText = await _jSRuntime
                        .InvokeAsync<string>("AESEncryptText", cryptogram.Cyphertext, key.Value!.ToString());
                }

                var result = new Cryptogram
                {
                    Cyphertext = encryptedText,
                    Iv = await _jSRuntime.InvokeAsync<string>("ExportIV", cryptogram.Cyphertext),
                    KeyId = key.Id,
                };
                
                await _jSRuntime.InvokeVoidAsync("DeleteIv", cryptogram.Cyphertext);

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message, ex);
            }
        }
    }
}
