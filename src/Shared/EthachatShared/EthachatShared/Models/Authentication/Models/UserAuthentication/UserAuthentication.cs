using EthachatShared.Models.Authentication.Models.AuthenticatedUserRepresentation.Claims;

namespace EthachatShared.Models.Authentication.Models.UserAuthentication
{
    public class UserAuthentication
    {
        public string Username { get; set; } = "N/A";
        public string? Password { get; set; }
        public List<UserClaimsDto>? Claims { get; set; }
        public string UserAgent { get; set; }
        public Guid UserAgentId { get; set; }
    }
}