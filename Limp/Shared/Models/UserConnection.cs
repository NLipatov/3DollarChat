using LimpShared.Encryption;

namespace ClientServerCommon.Models
{
    public class UserConnection
    {
        public Key RSAPublicKey { get; set; }
        public List<string> ConnectionIds { get; set; } = new();
    }
}
