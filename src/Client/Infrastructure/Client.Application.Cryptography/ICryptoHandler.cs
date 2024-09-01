using EthachatShared.Encryption;
using EthachatShared.Models.Cryptograms;

namespace Client.Application.Cryptography;

public interface ICryptoHandler
{
    public Task<BinaryCryptogram> Encrypt(byte[] data, Key key);
    public Task<BinaryCryptogram> Decrypt(BinaryCryptogram cryptogram, Key key);
}
