namespace Limp.Client.Pages.Contacts.Models
{
    public class Contact
    {
        public string Username { get; set; } = "Contact without name";
        public bool IsOnline { get; set; } = false;
        public DateTime LastMessage { get; set; } = DateTime.UtcNow;
    }
}
