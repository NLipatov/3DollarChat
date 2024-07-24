using System.Collections.Concurrent;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations
    .MessageService.Implementation.ContextManagers.AesKeyExchange;

public class KeyExchangeContextManager : IKeyExchangeContextManager
{
    private ConcurrentDictionary<string, KeyExchangeContext> Contexts { get; } = [];

    public void Add(string username, string rsa)
    {
        if (Contexts.TryGetValue(username, out var context))
        {
            context.Keys.Add(rsa);
        }
        else
        {
            Contexts.TryAdd(username, new KeyExchangeContext(rsa));
        }
    }

    public void Delete(string username, string rsa)
    {
        if (Contexts.TryGetValue(username, out var context))
        {
            context.Keys.Remove(rsa);
            if (context.Keys.Count == 0)
            {
                Contexts.Remove(username, out _);
            }
        }
    }

    public bool Contains(string username, string rsa)
    {
        if (Contexts.TryGetValue(username, out var context))
            return context.Keys.Any(x => x == rsa);

        return false;
    }
}