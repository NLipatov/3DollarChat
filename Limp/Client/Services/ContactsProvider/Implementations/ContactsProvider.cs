using Microsoft.JSInterop;
using System.Text.Json;
using Ethachat.Client.Pages.Contacts.Models;

namespace Ethachat.Client.Services.ContactsProvider.Implementations;

public class ContactsProvider : IContactsProvider
{
    public async Task AddContact(Contact storedContact, IJSRuntime jSRuntime)
    {
        List<Contact> contacts = await GetContacts(jSRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == storedContact.Username);
        if (target is not null)
        {
            contacts.Remove(target);
        }
        contacts.Add(storedContact);
        await SaveContacts(contacts, jSRuntime);
    }

    public async Task<List<Contact>> GetContacts(IJSRuntime jSRuntime)
    {
        await EnsureContactsItemExistsAsync(jSRuntime);
        string contactsSerialized = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "contacts");
        if (string.IsNullOrWhiteSpace(contactsSerialized))
            return new();

        List<Contact> contactsDeserialized = JsonSerializer.Deserialize<List<Contact>?>(contactsSerialized) ?? new();

        return contactsDeserialized;
    }

    public async Task UpdateContact(Contact storedContact, IJSRuntime jsRuntime)
    {
        List<Contact> contacts = await GetContacts(jsRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == storedContact.Username);
        if (target is not null)
        {
            contacts.Remove(target);
            contacts.Add(storedContact);
            await SaveContacts(contacts, jsRuntime);
        }
    }

    public async Task RemoveContact(Contact storedContact, IJSRuntime jSRuntime)
    {
        List<Contact> contacts = await GetContacts(jSRuntime);
        var target = contacts.FirstOrDefault(x => x.Username == storedContact.Username);
        if (target is not null)
        {
            contacts.Remove(target);
            await SaveContacts(contacts, jSRuntime);
        }
    }

    public async Task RemoveContact(string username, IJSRuntime jsRuntime)
    {
        List<Contact> contacts = await GetContacts(jsRuntime);
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

    public async Task SaveContacts(List<Contact> contacts, IJSRuntime jSRuntime)
    {
        string contactsSerialized = JsonSerializer.Serialize(contacts);
        await jSRuntime.InvokeVoidAsync("localStorage.setItem", "contacts", contactsSerialized);
    }
}
