using EthachatShared.Models.Authentication.Models.Credentials.Implementation;

namespace EthachatShared.Models.WebPushNotification
{
    public record NotificationSubscriptionDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public JwtPair? JwtPair { get; set; }
        public WebAuthnPair? WebAuthnPair { get; set; }
        public string? Url { get; set; }
        public string? P256dh { get; set; }
        public string? Auth { get; set; }
        public Guid UserAgentId { get; set; }
        public string? FirebaseRegistrationToken { get; set; }
    }
}
