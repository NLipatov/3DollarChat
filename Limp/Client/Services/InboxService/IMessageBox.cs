using Limp.Client.ClientOnlyModels;

namespace Limp.Client.Services.InboxService
{
    public interface IMessageBox
    {
        public List<ClientMessage> Messages { get; }

        /// <summary>
        /// Adds message to message box
        /// </summary>
        Task AddMessageAsync(ClientMessage message, bool isEncrypted = true);

        /// <summary>
        /// Adds messages to message box
        /// </summary>
        Task AddMessagesAsync(ClientMessage[] messages, bool isEncrypted = true);

        /// <summary>
        /// Marks message as seen
        /// </summary>
        public void OnSeen(Guid messageId);

        /// <summary>
        /// Marks message as delivered
        /// </summary>
        Task OnDelivered(Guid messageId);

        /// <summary>
        /// Marks message toast as shown
        /// </summary>
        void OnToastWasShown(Guid messageId);
    }
}