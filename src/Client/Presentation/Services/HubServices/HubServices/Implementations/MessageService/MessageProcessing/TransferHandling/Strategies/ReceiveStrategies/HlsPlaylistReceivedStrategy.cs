using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class HlsPlaylistReceivedStrategy(IMessageBox messageBox) : ITransferHandler<HlsPlaylistMessage>
{
    public Task HandleAsync(HlsPlaylistMessage eventMessage)
    {
        messageBox.AddMessage(new ClientMessage
        {
            Id = eventMessage.Id,
            Sender = eventMessage.Sender,
            Target = eventMessage.Target,
            Type = MessageType.HLSPlaylist,
            HlsPlaylist = new HlsPlaylist
            {
                M3U8Content = eventMessage.Playlist,
                Name = "video"
            }
        });
        return Task.CompletedTask;
    }
}