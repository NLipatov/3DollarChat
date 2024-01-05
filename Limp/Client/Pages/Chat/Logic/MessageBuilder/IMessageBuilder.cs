using EthachatShared.Models.Message;

namespace Ethachat.Client.Pages.Chat.Logic.MessageBuilder
{
    public interface IMessageBuilder
    {
        Task<Message> BuildMessageToBeSend(string plainMessageText, string topicName, string myName, Guid id, MessageType type);
    }
}