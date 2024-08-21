using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedHlsPlaylist(IMessageBox messageBox) : IStrategyHandler<HlsPlaylistMessage>
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