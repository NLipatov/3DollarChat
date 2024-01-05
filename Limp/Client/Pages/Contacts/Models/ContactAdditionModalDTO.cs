namespace Ethachat.Client.Pages.Contacts.Models
{
    public record class ContactAdditionModalDTO
    {
        public bool IsUserExists { get; set; } = false;
        public string ModalBodyText { get; set; } = string.Empty;
        public string NewContactUsername { get; set; } = string.Empty;
    }
}
