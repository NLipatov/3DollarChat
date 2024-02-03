using Ethachat.Client.Pages.Contacts.Models;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.ContactsProvider
{
    public interface IContactsProvider
    {
        Task<Contact?> GetContact(string username, IJSRuntime jSRuntime);
        Task<List<Contact>> GetContacts(IJSRuntime jSRuntime);
        Task AddContact(Contact storedContact, IJSRuntime jSRuntime);
        Task UpdateContact(Contact storedContact, IJSRuntime jsRuntime);
        Task RemoveContact(Contact storedContact, IJSRuntime jSRuntime);
        Task RemoveContact(string username, IJSRuntime jsRuntime);
        Task SaveContacts(List<Contact> contacts, IJSRuntime jSRuntime);
    }
}
