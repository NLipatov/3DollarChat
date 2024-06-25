using EthachatShared.Encryption;
using MessagePack;

namespace Ethachat.Client.ClientOnlyModels;

[MessagePackObject]
public class KeyMessage
{
    [Key(0)] public required Key Key { get; set; }
}