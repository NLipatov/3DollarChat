using Client.Application.Cryptography;
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

        public async Task<Cryptogram> Decrypt(Cryptogram cryptogram, Key key)
        {
            try
            {
                string decryptedMessage = string.Empty;
                if (!string.IsNullOrWhiteSpace(cryptogram.Cyphertext))
                {
                    await _jSRuntime.InvokeVoidAsync("ImportIV", cryptogram.Iv, cryptogram.Cyphertext);
                    await _jSRuntime.InvokeVoidAsync("importSecretKey",
                        (key.Value ?? throw new ApplicationException("Missing key")).ToString());
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

        public async Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key)
        {
            try
            {
                // var localKeyStorageService = new LocalStorageKeyStorage(_jSRuntime);
                // Key key = await localKeyStorageService.GetLastAcceptedAsync(contact, KeyType.Aes);
                //
                // if (key is null)
                //     throw new ApplicationException("Could not resolve a AES key for encryption.");

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