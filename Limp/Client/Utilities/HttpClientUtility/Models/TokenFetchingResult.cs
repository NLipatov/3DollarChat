using LimpShared.Authentification;

namespace Limp.Client.Utilities.HttpClientUtility.Models
{
    public class TokenFetchingResult
    {
        public string? Message { get; set; }
        public TokenAquisitionResult Result { get; set; }
        public JWTPair? JWTPair { get; set; }
    }
    public enum TokenAquisitionResult
    {
        Success,
        Fail
    }
}
