using ClientServerCommon.Models.Message;
using Limp.Client.Pages.PersonalChat.Logic.MessageBuilder;
using Limp.Client.Services.HubServices.MessageService;
using Limp.Client.Services.InboxService;
using Limp.Client.Services.UndeliveredMessagesStore;

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
            Message messageToSend = await _messageBuilder.BuildMessageToBeSend(text, targetGroup, myUsername, messageId);

            await AddAsUnreceived(text, targetGroup, myUsername, messageId);

            await AddToMessageBox(text, targetGroup, myUsername, messageId);

            await _messageService.SendMessage(messageToSend);
        }

        private async Task AddToMessageBox(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _messageBox.AddMessageAsync(new Message
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainTextPayload = text
            },
            isEncrypted: false);
        }

        private async Task AddAsUnreceived(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _undeliveredMessagesRepository.AddAsync(new Message
            {
                Id = messageId,
                Sender = myUsername,
                TargetGroup = targetGroup,
                PlainTextPayload = text
            });
        }
    }
}