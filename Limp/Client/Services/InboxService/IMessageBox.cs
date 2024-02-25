using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message;

namespace Ethachat.Client.Services.InboxService
{
    public interface IMessageBox
    {
        /// <summary>
        /// Is message with such Id already added to MessageBox
        /// </summary>
        bool Contains(Message message);
        
        public List<ClientMessage> Messages { get; }

        void Delete(string targetGroup);
        
        void Delete(Message message);

        /// <summary>
        /// Adds message to message box
        /// </summary>
        void AddMessage(ClientMessage message);

        /// <summary>
        /// Marks message as seen
        /// </summary>
        public void OnSeen(Guid messageId);

        /// <summary>
        /// Marks message as delivered
        /// </summary>
        Task OnDelivered(Guid messageId);
        
        /// <summary>
        /// Marks message as registered by server
        /// </summary>
        Task OnRegistered(Guid messageId);

        /// <summary>
        /// Marks message toast as shown
        /// </summary>
        void OnToastWasShown(Guid messageId);
    }
}