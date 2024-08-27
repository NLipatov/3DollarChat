using BlazorTemplater;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.UI.Chat.UI.Childs.MessageCollectionDispaying.Childs;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services;

public class MessageRenderer(
    IJSRuntime jsRuntime,
    IHubServiceSubscriptionManager hubServiceSubscriptionManager,
    IMessageBox messageBox,
    ICallbackExecutor callbackExecutor)
{
    public string RenderMessages(List<ClientMessage> messages)
    {
        foreach (var message in messages)
        {
            var messageHtml = new ComponentRenderer<SingleMessage>()
                .Set(m => m.Message, message)
                .AddService(jsRuntime)
                .AddService(hubServiceSubscriptionManager)
                .AddService(messageBox)
                .AddService(callbackExecutor)
                .Render();
        }

        return string.Empty;
    }
}