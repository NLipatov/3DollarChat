using EthachatShared.Encryption;

namespace EthachatShared.Models.Message.KeyTransmition;

public class AesOffer
{
    public Key key { get; set; }
    public string PassPhrase { get; set; }
}