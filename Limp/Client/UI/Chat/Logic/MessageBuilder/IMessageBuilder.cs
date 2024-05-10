using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.UI.Chat.Logic.MessageBuilder
{
    public interface IMessageBuilder
    {
        IAsyncEnumerable<TextMessage> BuildTextMessage(ClientMessage message);
    }
}