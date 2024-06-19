using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class HlsPlaylistHandler(IMessageBox messageBox) : ITransferHandler<ClientMessage>
{
    public Task HandleAsync(ClientMessage clientMessage)
    {
        messageBox.AddMessage(clientMessage);
        return Task.CompletedTask;
    }
}