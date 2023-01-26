using Limp.Shared.Models;

namespace Limp.Server.Hubs.MessageDispatching
{
    public static class MessageStore
    {
        public static List<Message> UnprocessedMessages { get; set; } = new();
    }
}
