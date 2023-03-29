using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubInteraction.Handlers.MessageDispatcherHub.AESOfferHandling;
using Limp.Client.TopicStorage;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubConnectionManagement.HubObservers.Implementations.MessageHub.MessageHubEventHandlers.ReceiveMessageEvent;

/// <summary>
/// Handles ReceiveMessage event, that can be triggered from server side
/// </summary>
public class ReceiveMessageHandler : IEventHandler<Message>
{
    private readonly ICryptographyService _cryptographyService;
    private readonly IAESOfferHandler _aESOfferHandler;
    private readonly IMessageBox _messageBox;
    private readonly HubConnection _messageDispatcherHub;

    public ReceiveMessageHandler
    (ICryptographyService cryptographyService,
    IAESOfferHandler aESOfferHandler,
    IMessageBox messageBox,
    HubConnection messageDispatcherHub)
    {
        _cryptographyService = cryptographyService;
        _aESOfferHandler = aESOfferHandler;
        _messageBox = messageBox;
        _messageDispatcherHub = messageDispatcherHub;
    }
    public async Task Handle(Message message)
    {
        if (message.Sender != "You")
        {
            if (_cryptographyService == null)
                throw new ArgumentException($"Please provide an instance of type {typeof(ICryptographyService)} as an argument.");

            if (message.Type == MessageType.AESOffer)
            {
                await SendMessage(await _aESOfferHandler.GetAESOfferResponse(message));
            }
        }

        await _messageBox.AddMessageAsync(message);

        await _messageDispatcherHub.SendAsync("MessageReceived", message.Id);

        //If we dont yet know a partner Public Key, we will request it from server side.
        await GetPartnerPublicKey(message.Sender!);
    }
    private async Task GetPartnerPublicKey(string partnersUsername)
    {
        if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == partnersUsername).Value == null)
        {
            await _messageDispatcherHub.SendAsync("GetAnRSAPublic", partnersUsername);
        }
    }

    private async Task SendMessage(Message message)
    {
        await _messageDispatcherHub.SendAsync("Dispatch", message);
    }
}
