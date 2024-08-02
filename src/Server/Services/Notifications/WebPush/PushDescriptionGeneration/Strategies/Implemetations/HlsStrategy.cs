using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implemetations;

public class HlsStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand CreateCommand<T>(T clientMessage) where T : IDestinationResolvable, ISourceResolvable
    {
        return new SendNotificationCommand
        {
            PushMessage = $"You've got a new video message from {clientMessage.Sender}",
            IsSendRequired = true
        };
    }
}