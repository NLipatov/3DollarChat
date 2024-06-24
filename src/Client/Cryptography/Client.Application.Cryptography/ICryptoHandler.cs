using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Client.Application.Cryptography;

public interface ICryptoHandler
{
    public Task<BinaryCryptogram> Encrypt<T>(T data, Key key);
    public Task<BinaryCryptogram> Decrypt(BinaryCryptogram cryptogram, Key key);
    public Task<Cryptogram> Encrypt(Cryptogram cryptogram, Key key);
    public Task<Cryptogram> Decrypt(Cryptogram cryptogram, Key key);
}
