namespace Ethachat.Client.UI.Contacts.Models
{
    public record class NewContactModel
    {
        public bool Exists { get; set; } = false;
        public string Username { get; set; } = string.Empty;
    }
}
