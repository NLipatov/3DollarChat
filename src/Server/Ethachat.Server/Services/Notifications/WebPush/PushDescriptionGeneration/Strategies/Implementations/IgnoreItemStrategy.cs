using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implementations;

public class IgnoreItemStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand CreateCommand<T>(T clientMessage)
        where T : IDestinationResolvable, ISourceResolvable, IWebPushNotice
    {
        return new SendNotificationCommand
        {
            IsSendRequired = false
        };
    }
}