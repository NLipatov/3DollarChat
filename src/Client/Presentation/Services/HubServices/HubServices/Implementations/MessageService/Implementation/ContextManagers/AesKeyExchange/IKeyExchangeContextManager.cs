namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.ContextManagers.AesKeyExchange;

public interface IKeyExchangeContextManager
{
    void Add(string username, string rsa);
    void Delete(string username, string rsa);
    public bool Contains(string username, string rsa);
}