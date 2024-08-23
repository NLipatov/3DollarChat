using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Types;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Models;

internal record HandlerArguments<T>
{
    internal required TransferDirection TransferDirection { get; init; }
    internal required IStrategyHandler<T> Handler { get; init; }
}