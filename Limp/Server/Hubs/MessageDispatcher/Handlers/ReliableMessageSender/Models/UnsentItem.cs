using EthachatShared.Models.Message;

namespace Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.Models;

public class UnsentItem
{
    public Message Message { get; set; }
    public bool ACK { get; set; }
    public TimeSpan Backoff { get; set; } = TimeSpan.FromSeconds(10);
    public DateTime ResendAfter { get; set; } = DateTime.MinValue;
}