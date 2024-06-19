using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Strategies;

public class IgnoreItemStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand CreateCommand<T>(T clientMessage) where T : IDestinationResolvable, ISourceResolvable
    {
        return new SendNotificationCommand
        {
            IsSendRequired = false
        };
    }
}