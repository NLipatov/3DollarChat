using LimpShared.Authentification;

namespace ClientServerCommon.Models.Login
{
    public class AuthResult
    {
        public string? Message { get; set; }
        public AuthResultType? Result { get; set; } = null;
        public JWTPair? JWTPair { get; set; }
    }
}
