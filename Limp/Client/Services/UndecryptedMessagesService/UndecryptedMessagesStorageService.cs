using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService;
using Ethachat.Client.Services.UndecryptedMessagesService.Models;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.UndecryptedMessagesService;

public class UndecryptedMessagesStorageService : IUndecryptedMessagesStorageService<UndecryptedItem>
{
    private readonly IHubServiceSubscriptionManager _serviceSubscriptionManager;
    private readonly IMessageService _messageService;
    private readonly IAuthenticationHandler _authenticationHandler;
    private readonly Guid _id;
    private ConcurrentDictionary<string, List<UndecryptedItem>> _undecryptedItems = [];

    public UndecryptedMessagesStorageService(IHubServiceSubscriptionManager serviceSubscriptionManager,
        IMessageService messageService,
        IAuthenticationHandler authenticationHandler)
    {
        _id = Guid.NewGuid();
        _serviceSubscriptionManager = serviceSubscriptionManager;
        _messageService = messageService;
        _authenticationHandler = authenticationHandler;
        serviceSubscriptionManager.AddCallback<UndecryptedItem>(Add, "OnDecryptionFailure", _id);
        serviceSubscriptionManager.AddCallback<string>(AskForResend, "AESUpdated", _id);
    }

    public void Add(UndecryptedItem item)
    {
        _undecryptedItems.AddOrUpdate(item.Sender,
            _ => [item],
            (_, existingData) =>
            {
                existingData.Add(item);
                return existingData;
            });
    }

    private async Task AskForResend(string key)
    {
        var items = _undecryptedItems.GetValueOrDefault(key) ?? [];
        foreach (var item in items)
        {
            await _messageService.SendMessage(new ClientMessage
            {
                Sender = await _authenticationHandler.GetUsernameAsync(),
                Target = item.Sender,
                Id = item.Id,
                Type = MessageType.ResendRequest
            });
        }
    }

    public void Dispose()
    {
        _serviceSubscriptionManager.RemoveComponentCallbacks(_id);
    }
}