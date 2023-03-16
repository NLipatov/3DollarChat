#nullable disable
using LimpShared.Encryption;

namespace ClientServerCommon.Models.Message;

public class Handshake
{
    public Key OfferedAESKey { get; set; }
}
