using ClientServerCommon.Models.Message;
using Limp.Client.Pages.PersonalChat.Logic.MessageBuilder;
using Limp.Client.Services.HubServices.MessageService;
using Limp.Client.Services.InboxService;
using Limp.Client.Services.UndeliveredMessagesStore;
using Limp.Client.Services.UndeliveredMessagesStore.Implementation;

namespace Limp.Client.Pages.PersonalChat.Logic.MessageSender
{
    public class MessageSender : IMessageSender
    {
        private readonly IMessageBuilder _messageBuilder;
        private readonly IMessageService _messageService;
        private readonly IMessageBox _messageBox;
        private readonly IUndeliveredMessagesRepository _undeliveredMessagesRepository;

        public MessageSender
            (IMessageBuilder messageBuilder, 
            IMessageService messageService, 
            IMessageBox messageBox, 
            IUndeliveredMessagesRepository undeliveredMessagesRepository)
        {
            _messageBuilder = messageBuilder;
            _messageService = messageService;
            _messageBox = messageBox;
            _undeliveredMessagesRepository = undeliveredMessagesRepository;
        }
        public async Task SendMessageAsync(string text, string targetGroup, string myUsername)
        {
            Guid messageId = Guid.NewGuid();

            await _undeliveredMessagesRepository.AddAsync(new Message { Id = messageId, PlainTextPayload = text, Sender = myUsername, TargetGroup = targetGroup });

            Message messageToSend = await _messageBuilder.BuildMessageToBeSend(text, targetGroup, myUsername, messageId);

            await _messageService.SendMessage(messageToSend);

            messageToSend.PlainTextPayload = text;
            messageToSend.Sender = "You";
            await _messageBox.AddMessageAsync(messageToSend, isEncrypted: false);
        }
    }
}