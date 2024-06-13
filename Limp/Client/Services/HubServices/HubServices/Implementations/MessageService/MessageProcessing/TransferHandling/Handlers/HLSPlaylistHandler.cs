using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class HlsPlaylistHandler(IMessageBox messageBox) : ITransferHandler
{
    public Task HandleAsync(object transfer)
    {
        messageBox.AddMessage(transfer as ClientMessage ?? throw new ArgumentException());
        return Task.CompletedTask;
    }
}