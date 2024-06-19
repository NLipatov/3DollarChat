using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Strategies;

public class TextMessageStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand Process<T>(T clientMessage) where T : IDestinationResolvable, ISourceResolvable
    {
        return new SendNotificationCommand
        {
            PushMessage = $"You've got a new text message from {clientMessage.Sender}",
            IsSendRequired = true
        };
    }
}