using LimpShared.Authentification;

namespace Limp.Shared.Models.Login
{
    public class LogInResult
    {
        public string? Message { get; set; }
        public LogInStatus? Result { get; set; } = null;
        public JWTPair? JWTPair { get; set; }
    }
}
