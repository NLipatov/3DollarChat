namespace EthachatShared.Models.Authentication.Models
{
    public class RefreshToken
    {
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public string? Token { get; set; }
    }
}