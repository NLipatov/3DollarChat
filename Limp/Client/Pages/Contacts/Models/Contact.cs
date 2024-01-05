namespace Ethachat.Client.Pages.Contacts.Models
{
    public class Contact
    {
        public string Username { get; set; } = "Contact without a name";
        public bool IsOnline { get; set; } = false;
        public DateTime LastMessage { get; set; } = DateTime.UtcNow;
        public bool IsKeyReady { get; set; } = false;
        public int UnreadedMessagesCount { get; set; } = 0;
    }
}
