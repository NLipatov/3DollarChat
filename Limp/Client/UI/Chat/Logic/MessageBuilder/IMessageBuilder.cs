using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;

namespace Ethachat.Client.UI.Chat.Logic.MessageBuilder
{
    public interface IMessageBuilder
    {
        IAsyncEnumerable<Message> BuildTextMessageToBeSend(ClientMessage message);
    }
}