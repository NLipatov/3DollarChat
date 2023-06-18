using LimpShared.Models.Message;

namespace Limp.Client.Pages.Chat.Logic.MessageBuilder
{
    public interface IMessageBuilder
    {
        Task<Message> BuildMessageToBeSend(string plainMessageText, string topicName, string myName, Guid id);
    }
}