namespace Ethachat.Client.UI.Contacts.Models
{
    public class Contact
    {
        public string Username { get; set; } = "Contact without a name";
        public bool IsOnline { get; set; }
        public DateTime LastMessage { get; set; } = DateTime.UtcNow;
        public bool IsKeyReady { get; set; }
        public int UnreadedMessagesCount { get; set; }
        public string TrustedPassphrase { get; set; } = string.Empty;
        public bool IsTrusted { get; set; }
    }
}
