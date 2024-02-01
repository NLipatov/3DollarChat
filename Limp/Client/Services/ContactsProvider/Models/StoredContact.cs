namespace Ethachat.Client.Services.ContactsProvider.Models;

public record StoredContact
{
    public required string Username { get; set; }
    public string TrustedPassphrase { get; set; } = string.Empty;
    public bool IsTrusted { get; set; }
}