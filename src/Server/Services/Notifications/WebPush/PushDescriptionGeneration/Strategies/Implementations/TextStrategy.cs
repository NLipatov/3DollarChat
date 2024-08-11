using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implementations;

/// <summary>
/// Generates push text message for <see cref="TextMessage"/>
/// </summary>
public class TextStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand CreateCommand<T>(T clientMessage)
        where T : IDestinationResolvable, ISourceResolvable, IWebPushNotice
    {
        return new SendNotificationCommand
        {
            PushMessage = $"Text message from {clientMessage.Sender}",
            IsSendRequired = clientMessage.IsPushRequired
        };
    }
}