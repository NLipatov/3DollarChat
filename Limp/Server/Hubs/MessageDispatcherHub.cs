using ClientServerCommon.Models.Message;
using Limp.Server.Hubs.MessageDispatching;
using Limp.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using Limp.Server.Hubs.UsersConnectedManaging.EventHandling;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Server.Utilities.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs
{
    public class MessageDispatcherHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IMessageBrokerService _messageBrokerService;
        private readonly IUserConnectedHandler<MessageDispatcherHub> _userConnectedHandler;

        public MessageDispatcherHub
        (IServerHttpClient serverHttpClient,
        IMessageBrokerService messageBrokerService,
        IUserConnectedHandler<MessageDispatcherHub> userConnectedHandler)
        {
            _serverHttpClient = serverHttpClient;
            _messageBrokerService = messageBrokerService;
            _userConnectedHandler = userConnectedHandler;
        }

        private static bool IsClientConnectedToHub(string username)
        {
            lock(InMemoryHubConnectionStorage.MessageDispatcherHubConnections)
            {
                return InMemoryHubConnectionStorage.MessageDispatcherHubConnections.Any(x => x.Username == username);
            }
        }

        public async override Task OnConnectedAsync()
            => _userConnectedHandler.OnConnect(Context.ConnectionId);

        public async override Task OnDisconnectedAsync(Exception? exception)
            => _userConnectedHandler.OnDisconnect(Context.ConnectionId);

        public async Task SetUsername(string accessToken)
            => await _userConnectedHandler.OnUsernameResolved(Context.ConnectionId, accessToken, Groups.AddToGroupAsync, Clients.Caller.SendAsync);


        /// <summary>
        /// Checks if target user is connected to the same hub.
        /// If so: sends him a message.
        /// If not: sends message to message broker.
        /// </summary>
        /// <param name="message">A message that needs to be send</param>
        /// <exception cref="ApplicationException"></exception>
        public async Task Dispatch(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.TargetGroup))
                throw new ArgumentException("Invalid target group of a message.");

            switch (IsClientConnectedToHub(message.TargetGroup))
            {
                case true:
                    await Deliver(message);
                    break;

                case false:
                    await Task.Delay(1000);
                    await Dispatch(message);
                    break;

                default:
                    throw new ApplicationException("Could not dispatch a message");
            }
        }

        /// <summary>
        /// Sends message to a message broker system
        /// </summary>
        /// <param name="message">Message to ship</param>
        public async Task Ship(Message message)
        {
            await _messageBrokerService.ProduceAsync(message);
        }

        /// <summary>
        /// Deliver message to connected Hub client
        /// </summary>
        /// <param name="message">Message for delivery</param>
        public async Task Deliver(Message message)
        {
            MessageStore.UnprocessedMessages.Add(message.Clone());

            string targetGroup = message.TargetGroup;

            message.Topic = message.Sender;
            await Clients.Group(targetGroup).SendAsync("ReceiveMessage", message);
            message.Topic = message.TargetGroup;
            message.Sender = "You";
            await Clients.Caller.SendAsync("ReceiveMessage", message);
            //In the other case we need some message storage to be implemented to store a not delivered messages and remove them when they are delivered.
        }

        public async Task MessageReceived(Guid messageId)
        {
            Message? deliveredMessage = MessageStore.UnprocessedMessages.FirstOrDefault(x => x.Id == messageId);
            if (deliveredMessage != null)
            {
                MessageStore.UnprocessedMessages.Remove(deliveredMessage);
                await Clients.Group(deliveredMessage.Sender).SendAsync("MessageWasReceivedByRecepient", messageId);
            }
        }
        public async Task GetAnRSAPublic(string username)
        {
            string? pubKey = await _serverHttpClient.GetAnRSAPublicKey(username);
            await Clients.Caller.SendAsync("ReceivePublicKey", username, pubKey);
        }
    }
}