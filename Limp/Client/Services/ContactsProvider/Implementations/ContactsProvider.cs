using Microsoft.JSInterop;
using System.Text.Json;

namespace Ethachat.Client.Services.ContactsProvider.Implementations;

public class ContactsProvider : IContactsProvider
{
    public async Task AddContact(string username, IJSRuntime jSRuntime)
    {
        List<string>? contacts = await GetContacts(jSRuntime);
        contacts.Add(username);
        await SetContactItem(jSRuntime, contacts);
    }

    public async Task<List<string>> GetContacts(IJSRuntime jSRuntime)
    {
        await EnsureContactsItemExistsAsync(jSRuntime);
        string contactsSerialized = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "contacts");
        if (string.IsNullOrWhiteSpace(contactsSerialized))
            return new();

        List<string> contactsDeserialized = JsonSerializer.Deserialize<List<string>?>(contactsSerialized) ?? new();

        if (contactsDeserialized == null)
            throw new ApplicationException("'contact' item cannot be accessed due to unhandled exception.");

        return contactsDeserialized;
    }

    public async Task RemoveContact(string username, IJSRuntime jSRuntime)
    {
        List<string>? contacts = await GetContacts(jSRuntime);
        if (contacts.Any(x => x == username))
        {
            contacts.Remove(username);
            await SetContactItem(jSRuntime, contacts);
        }
    }

    private async Task EnsureContactsItemExistsAsync(IJSRuntime jSRuntime)
    {
        string? itemValue = await jSRuntime.InvokeAsync<string?>("localStorage.getItem", "contacts");

        if (string.IsNullOrWhiteSpace(itemValue))
            await SetContactItem(jSRuntime, new());
    }

    private async Task SetContactItem(IJSRuntime jSRuntime, List<string> contacts)
    {
        string contactsSerialized = JsonSerializer.Serialize(contacts);
        await jSRuntime.InvokeVoidAsync("localStorage.setItem", "contacts", contactsSerialized);
    }
}
