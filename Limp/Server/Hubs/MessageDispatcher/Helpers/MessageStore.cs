using ClientServerCommon.Models.Message;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers
{
    public static class MessageStore
    {
        public static List<Message> UnprocessedMessages { get; set; } = new();
    }
}
