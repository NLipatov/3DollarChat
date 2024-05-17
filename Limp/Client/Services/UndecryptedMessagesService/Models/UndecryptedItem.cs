using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Client.Services.UndecryptedMessagesService.Models;

public record UndecryptedItem : ISourceResolvable, IIdentifiable, IDestinationResolvable
{
    public required Guid Id { get; set; }
    public required string Target { get; set; }
    public required string Sender { get; set; }
}