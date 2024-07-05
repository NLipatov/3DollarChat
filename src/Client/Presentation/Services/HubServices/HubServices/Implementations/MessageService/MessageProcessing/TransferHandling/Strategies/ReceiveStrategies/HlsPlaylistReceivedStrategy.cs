using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class HlsPlaylistReceivedStrategy(IMessageBox messageBox) : ITransferHandler<HlsPlaylistMessage>
{
    public Task HandleAsync(HlsPlaylistMessage playtlistMesssage)
    {
        messageBox.AddMessage(new ClientMessage
        {
            Id = playtlistMesssage.Id,
            Sender = playtlistMesssage.Sender,
            Target = playtlistMesssage.Target,
            Type = MessageType.HLSPlaylist,
            HlsPlaylist = new HlsPlaylist
            {
                M3U8Content = playtlistMesssage.Playlist,
                Name = "video"
            }
        });
        return Task.CompletedTask;
    }
}