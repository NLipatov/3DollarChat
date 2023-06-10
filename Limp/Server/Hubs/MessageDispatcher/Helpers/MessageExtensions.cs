using ClientServerCommon.Models.Message;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers
{
    internal static class MessageExtensions
    {
        /// <summary>
        /// For the receiver of this personal message, sender is a topic
        /// and
        /// For the sender receiver is a topic
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static Message ToReceiverRepresentation(this Message message)
        {
            message.TargetGroup = message.Sender;
            return message;
        }

        /// <summary>
        /// Sending a sender a message that indicating him, that hub has processed his message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static Message ToSenderRepresentation(this Message message)
        {
            message.Sender = "You";
            return message;
        }
    }
}
