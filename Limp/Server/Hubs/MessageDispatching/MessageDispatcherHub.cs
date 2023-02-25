using ClientServerCommon.Models.Message;
using Limp.Server.Hubs.UserStorage;
using Limp.Server.Utilities.HttpMessaging;
using Limp.Server.Utilities.Kafka;
using LimpShared.Authentification;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.MessageDispatching
{
    public class MessageDispatcherHub : Hub
    {
        private readonly IServerHttpClient _serverHttpClient;
        private readonly IMessageBrokerService _messageBrokerService;

        public MessageDispatcherHub
            (IServerHttpClient serverHttpClient,
            IMessageBrokerService messageBrokerService)
        {
            _serverHttpClient = serverHttpClient;
            _messageBrokerService = messageBrokerService;
        }

        private static bool IsClientConnectedToHub(string username) => InMemoryUsersStorage.UserConnections.Any(x => x.Username == username);

        /// <summary>
        /// Checks if target user is connected to the same hub.
        /// If so: sends him a message.
        /// If not: sends message to message broker.
        /// </summary>
        /// <param name="message">A message that needs to be send</param>
        /// <exception cref="ApplicationException"></exception>
        public async Task Dispatch(Message message)
        {
            switch (IsClientConnectedToHub(message.TargetGroup))
            {
                case true:
                    await Deliver(message);
                    break;

                case false:
                    await Ship(message);
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

        public async Task SetUsername(string accessToken)
        {
            TokenRelatedOperationResult usernameRequest = await _serverHttpClient.GetUserNameFromAccessTokenAsync(accessToken);

            var username = usernameRequest.Username;

            if (InMemoryUsersStorage.UserConnections.Any(x => x.Username == username))
            {
                InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds.Add(Context.ConnectionId);

                foreach (var connection in InMemoryUsersStorage.UserConnections.First(x => x.Username == username).ConnectionIds)
                {
                    await Groups.AddToGroupAsync(connection, username);
                }
            }
            else
            {
                InMemoryUsersStorage
                    .UserConnections
                    .First(x => x.ConnectionIds.Contains(Context.ConnectionId)).Username = username;
            }

            await Clients.Caller.SendAsync("OnMyNameResolve", username);
        }
    }
}