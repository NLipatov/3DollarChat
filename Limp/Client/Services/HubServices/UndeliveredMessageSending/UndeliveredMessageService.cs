using Limp.Client.ClientOnlyModels;
using Limp.Client.Pages.Chat.Logic.MessageBuilder;
using Limp.Client.Services.CloudKeyService;
using Limp.Client.Services.HubServices.CommonServices.SubscriptionService;
using Limp.Client.Services.HubServices.MessageService;
using Limp.Client.Services.UndeliveredMessagesStore;
using LimpShared.Encryption;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Message;

namespace Limp.Client.Services.HubServices.UndeliveredMessageSending
{
    public class UndeliveredMessageService : IDisposable, IUndeliveredMessageService
    {
        private readonly IHubServiceSubscriptionManager _hubServiceSubscriptionManager;
        private readonly IBrowserKeyStorage _browserKeyStorage;
        private readonly IUndeliveredMessagesRepository _undeliveredMessagesRepository;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IMessageService _messageService;

        private Guid ComponentId { get; set; }
        private bool IsSubbedAlready { get; set; } = false;

        public UndeliveredMessageService
            (IHubServiceSubscriptionManager hubServiceSubscriptionManager,
            IBrowserKeyStorage browserKeyStorage,
            IUndeliveredMessagesRepository undeliveredMessagesRepository,
            IMessageBuilder messageBuilder,
            IMessageService messageService)
        {
            ComponentId = Guid.NewGuid();
            _hubServiceSubscriptionManager = hubServiceSubscriptionManager;
            _browserKeyStorage = browserKeyStorage;
            _undeliveredMessagesRepository = undeliveredMessagesRepository;
            _messageBuilder = messageBuilder;
            _messageService = messageService;
        }

        public void SubscribeToUsersOnlineUpdate()
        {
            if (!IsSubbedAlready)
            {
                _hubServiceSubscriptionManager.AddCallback<UserConnectionsReport>(SendToOnlineUsersUndelivered, "ReceiveOnlineUsers", ComponentId);
                IsSubbedAlready = true;
            }
        }

        private async Task SendToOnlineUsersUndelivered(UserConnectionsReport userConnectionsReport)
        {
            foreach (var user in userConnectionsReport.UserConnections)
            {
                await Console.Out.WriteLineAsync($"Sending undelivered for: {user.Username}");
                await SendUndelivered(user.Username);
            }
        }

        private async Task SendUndelivered(string username)
        {
            //await Console.Out.WriteLineAsync($"Sending undelivered for {username}");
            Key? AESKey = await _browserKeyStorage.GetAESKeyForChat(username);
            if (AESKey != null)
            {
                List<ClientMessage> undelivered = await _undeliveredMessagesRepository.GetUndeliveredAsync();
                undelivered = undelivered.Where(x => x.TargetGroup == username).ToList();

                if (undelivered.Count == 0)
                    return;

                List<Message> toBeSend = new(undelivered.Count);

                foreach (var message in undelivered)
                {
                    toBeSend.Add(await _messageBuilder.BuildMessageToBeSend
                        (message.PlainText ?? string.Empty,
                        message.TargetGroup,
                        message.Sender,
                        message.Id));
                }

                foreach (var message in toBeSend)
                {
                    await _messageService.SendMessage(message);
                }
            }
        }

        public void Dispose()
        {
            _hubServiceSubscriptionManager.RemoveComponentCallbacks(ComponentId);
        }
    }
}
