namespace Limp.Client.Services.HubServices.HubServiceContract
{
    public interface IHubService
    {
        Task<Exception?> OnConnectionLost(Exception? ex);
    }
}
