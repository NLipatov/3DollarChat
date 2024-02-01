using Ethachat.Client.Services.ContactsProvider.Models;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.ContactsProvider
{
    public interface IContactsProvider
    {
        Task<List<StoredContact>> GetContacts(IJSRuntime jSRuntime);
        Task AddContact(StoredContact storedContact, IJSRuntime jSRuntime);
        Task UpdateContact(StoredContact storedContact, IJSRuntime jsRuntime);
        Task RemoveContact(StoredContact storedContact, IJSRuntime jSRuntime);
        Task RemoveContact(string username, IJSRuntime jsRuntime);
        Task SaveContacts(List<StoredContact> contacts, IJSRuntime jSRuntime);
    }
}
