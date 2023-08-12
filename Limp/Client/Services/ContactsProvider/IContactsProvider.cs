using Microsoft.JSInterop;

namespace Limp.Client.Services.ContactsProvider
{
    public interface IContactsProvider
    {
        Task<List<string>> GetContacts(IJSRuntime jSRuntime);
        Task AddContact(string username, IJSRuntime jSRuntime);
        Task RemoveContact(string username, IJSRuntime jSRuntime);
    }
}
