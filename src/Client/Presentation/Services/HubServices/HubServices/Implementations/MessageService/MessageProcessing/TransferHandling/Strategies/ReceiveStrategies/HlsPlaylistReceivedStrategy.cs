using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class HlsPlaylistReceivedStrategy(IMessageBox messageBox) : ITransferHandler<HlsPlaylistMessage>
{
    public Task HandleAsync(HlsPlaylistMessage playlistMessage)
    {
        messageBox.AddMessage(new ClientMessage
        {
            Id = playlistMessage.Id,
            Sender = playlistMessage.Sender,
            Target = playlistMessage.Target,
            Type = MessageType.HLSPlaylist,
            HlsPlaylist = new HlsPlaylist
            {
                M3U8Content = playlistMessage.Playlist,
                Name = "video"
            }
        });
        return Task.CompletedTask;
    }
}