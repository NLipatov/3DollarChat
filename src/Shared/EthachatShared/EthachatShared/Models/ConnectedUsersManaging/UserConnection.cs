#nullable disable
using EthachatShared.Encryption;

namespace EthachatShared.Models.ConnectedUsersManaging
{
    public class UserConnection
    {
        public string Username { get; set; }
        public Key RsaPublicKey { get; set; }
        public IList<string> ConnectionIds { get; set; }
        public List<string> UsersHubConnectionIds { get; set; } = new();
        public List<string> MessageHubConnectionIds { get; set; } = new();
    }
}
