using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;

namespace Client.Application.Cryptography;

public interface ICryptoHandler
{
    public Task<BinaryCryptogram> Encrypt<T>(T data, Key key);
    public Task<BinaryCryptogram> Decrypt(BinaryCryptogram cryptogram, Key key);
    public Task<TextCryptogram> Encrypt(TextCryptogram textCryptogram, Key key);
    public Task<TextCryptogram> Decrypt(TextCryptogram textCryptogram, Key key);
}
