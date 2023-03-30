using LimpShared.Encryption;

namespace ClientServerCommon.Models
{
    public class UserConnection
    {
        public string Username { get; set; }
        public Key RSAPublicKey { get; set; }
        public List<string> ConnectionIds { get; set; }
    }
}
