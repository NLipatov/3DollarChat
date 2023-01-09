using Limp.Client.TopicStorage;
using Limp.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.HubInteraction
{
    public class HubInteractor
    {
        public HubInteractor(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }
        private HubConnection? messageDispatcherHub;
        private List<Guid> subscriptions = new List<Guid>();
        private readonly NavigationManager _navigationManager;

        public async Task<HubConnection> ConnectToMessageDispatcherHubAsync(string accessToken, Action<Message>? onMessageReceive = null, Action<string>? onUsernameResolve = null)
        {
            if(onMessageReceive != null)
            {
                Guid subscriptionId = MessageBox.Subsctibe(onMessageReceive);
                subscriptions.Add(subscriptionId);
            }

            messageDispatcherHub = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/messageDispatcherHub"))
            .Build();

            messageDispatcherHub.On<Message>("ReceiveMessage", message =>
            {
                MessageBox.AddMessage(message);
            });

            if(onUsernameResolve != null)
            {
                messageDispatcherHub.On<string>("OnMyNameResolve", username =>
                {
                    onUsernameResolve(username);
                });
            }

            await messageDispatcherHub.StartAsync();

            await messageDispatcherHub.SendAsync("SetUsername", accessToken);

            return messageDispatcherHub;
        }

        public List<Message> LoadStoredMessages(string sender)
        {
            return MessageBox.FetchMessagesFromMessageBox(sender);
        }

        public async Task SendMessage(Message message)
        {
            if(messageDispatcherHub != null)
                await messageDispatcherHub.SendAsync("Dispatch", message);
        }

        public bool isMessageHubConnected()
        {
            if(messageDispatcherHub == null)
                return false;

            return messageDispatcherHub.State == HubConnectionState.Connected;
        }

        public async Task DisposeAsync()
        {
            await messageDispatcherHub.DisposeAsync();
            foreach (var subscription in subscriptions)
            {
                MessageBox.Unsubscribe(subscription);
            }
        }
    }
}
