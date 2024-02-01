using Microsoft.JSInterop;
using System.Text.Json;
using Ethachat.Client.Services.ContactsProvider.Models;

namespace Ethachat.Client.Services.ContactsProvider.Implementations;

public class ContactsProvider : IContactsProvider
{
    public async Task AddContact(StoredContact storedContact, IJSRuntime jSRuntime)
    {
        List<StoredContact> contacts = await GetContacts(jSRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == storedContact.Username);
        if (target is not null)
        {
            contacts.Remove(target);
        }
        contacts.Add(storedContact);
        await SaveContacts(contacts, jSRuntime);
    }

    public async Task<List<StoredContact>> GetContacts(IJSRuntime jSRuntime)
    {
        await EnsureContactsItemExistsAsync(jSRuntime);
        string contactsSerialized = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "contacts");
        if (string.IsNullOrWhiteSpace(contactsSerialized))
            return new();

        List<StoredContact> contactsDeserialized = JsonSerializer.Deserialize<List<StoredContact>?>(contactsSerialized) ?? new();

        return contactsDeserialized;
    }

    public async Task UpdateContact(StoredContact storedContact, IJSRuntime jsRuntime)
    {
        List<StoredContact> contacts = await GetContacts(jsRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == storedContact.Username);
        if (target is not null)
        {
            contacts.Remove(target);
            contacts.Add(storedContact);
            await SaveContacts(contacts, jsRuntime);
        }
    }

    public async Task RemoveContact(StoredContact storedContact, IJSRuntime jSRuntime)
    {
        List<StoredContact> contacts = await GetContacts(jSRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == storedContact.Username);
        if (target is not null)
        {
            contacts.Remove(target);
            await SaveContacts(contacts, jSRuntime);
        }
    }

    public async Task RemoveContact(string username, IJSRuntime jsRuntime)
    {
        List<StoredContact> contacts = await GetContacts(jsRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == username);
        if (target is not null)
        {
            contacts.Remove(target);
            await SaveContacts(contacts, jsRuntime);
        }
    }

    private async Task EnsureContactsItemExistsAsync(IJSRuntime jSRuntime)
    {
        string? itemValue = await jSRuntime.InvokeAsync<string?>("localStorage.getItem", "contacts");

        if (string.IsNullOrWhiteSpace(itemValue))
            await SaveContacts(new(), jSRuntime);
    }

    public async Task SaveContacts(List<StoredContact> contacts, IJSRuntime jSRuntime)
    {
        string contactsSerialized = JsonSerializer.Serialize(contacts);
        await jSRuntime.InvokeVoidAsync("localStorage.setItem", "contacts", contactsSerialized);
    }
}
