using Ethachat.Client.ClientOnlyModels;
using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies.Implemetations;

/// <summary>
/// Generates push text message for <see cref="HlsPlaylistMessage"/>
/// </summary>
public class HlsStrategy : IPushItemMessageStrategy
{
    public SendNotificationCommand CreateCommand<T>(T clientMessage) where T : IDestinationResolvable, ISourceResolvable
    {
        return new SendNotificationCommand
        {
            PushMessage = $"Video from {clientMessage.Sender}",
            IsSendRequired = true
        };
    }
}