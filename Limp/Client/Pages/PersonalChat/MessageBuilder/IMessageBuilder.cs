using ClientServerCommon.Models.Message;

namespace Limp.Client.Pages.PersonalChat.MessageBuilder
{
    public interface IMessageBuilder
    {
        Task<Message> BuildMessageToBeSend(string plainMessageText, string topicName, string myName);
    }
}