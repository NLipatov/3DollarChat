namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService;

public interface ILoggingService : IHubService
{
    Task LogException(Exception exception);
}