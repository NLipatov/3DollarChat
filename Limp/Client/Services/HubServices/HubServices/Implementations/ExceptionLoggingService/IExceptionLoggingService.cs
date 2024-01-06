namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.ExceptionLoggingService;

public interface IExceptionLoggingService : IHubService
{
    Task LogException(Exception exception);
}