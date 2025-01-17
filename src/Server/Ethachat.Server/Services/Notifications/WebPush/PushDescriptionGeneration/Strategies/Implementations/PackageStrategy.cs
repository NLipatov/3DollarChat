using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.DataTransfer;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implementations;

/// <summary>
/// Generates push text message for <see cref="Package"/>
/// </summary>
public class PackageStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand CreateCommand<T>(T clientMessage)
        where T : IDestinationResolvable, ISourceResolvable, IWebPushNotice
    {
        return new SendNotificationCommand
        {
            PushMessage = $"File from {clientMessage.Sender}",
            IsSendRequired = clientMessage.IsPushRequired
        };
    }
}