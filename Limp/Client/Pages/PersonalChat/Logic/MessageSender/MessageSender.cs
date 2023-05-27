using ClientServerCommon.Models.Message;
using Limp.Client.Pages.PersonalChat.Logic.MessageBuilder;
using Limp.Client.Services.HubServices.MessageService;
using Limp.Client.Services.InboxService;

namespace Limp.Client.Pages.PersonalChat.Logic.MessageSender
{
    public class MessageSender : IMessageSender
    {
        private readonly IMessageBuilder _messageBuilder;
        private readonly IMessageService _messageService;
        private readonly IMessageBox _messageBox;

        public MessageSender(IMessageBuilder messageBuilder, IMessageService messageService, IMessageBox messageBox)
        {
            _messageBuilder = messageBuilder;
            _messageService = messageService;
            _messageBox = messageBox;
        }
        public async Task SendMessageAsync(string text, string targetGroup, string myUsername)
        {
            Message messageToSend = await _messageBuilder.BuildMessageToBeSend(text, targetGroup, myUsername);

            await _messageService.SendMessage(messageToSend);

            messageToSend.Payload = text;
            messageToSend.Sender = "You";
            await _messageBox.AddMessageAsync(messageToSend, isEncrypted: false);
        }
    }
}