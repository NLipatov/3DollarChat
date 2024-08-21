using Client.Application.Gateway;

namespace Ethachat.Client.Services.HubServices.HubServices
{
    public interface IHubService
    {
        Task<IGateway> GetHubConnectionAsync();
    }
}
