using Ethachat.Server.Services.Notifications.WebPush.Commands;
using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush.PushDescriptionGeneration.Strategies;

public interface IPushItemMessageStrategy
{
    SendNotificationCommand CreateCommand<T>(T clientMessage) where T : IDestinationResolvable, ISourceResolvable;
}