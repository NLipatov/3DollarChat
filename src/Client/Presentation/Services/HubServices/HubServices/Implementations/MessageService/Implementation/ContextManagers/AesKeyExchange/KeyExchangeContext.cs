namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.ContextManagers.AesKeyExchange;

public class KeyExchangeContext(string rsa)
{
    public HashSet<string> Keys { get; set; } = [rsa];
}