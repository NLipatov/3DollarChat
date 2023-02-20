using LimpShared.Authentification;

namespace ClientServerCommon.Models.Login
{
    public class AuthResult
    {
        public string? Message { get; set; }
        public AuthResultType? Result { get; set; }
        public JWTPair? JWTPair { get; set; }
    }
}
