using ClientServerCommon.Models.Message;
using System.Collections.Concurrent;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.UndeliveredMessagesRegistry;

public class UndeliveredMessagesStorer : IUndeliveredMessagesStorer
{
    private ConcurrentDictionary<string, List<Message>> userUndeliveredMessagesKV { get; set; } = new();
    public void Add(Message message)
    {
        if (string.IsNullOrWhiteSpace(message.TargetGroup))
            return;

        bool keyExists = userUndeliveredMessagesKV.TryGetValue
            (message.TargetGroup, out List<Message>? undeliveredMessages);

        if (!keyExists)
            userUndeliveredMessagesKV.TryAdd(message.TargetGroup, new List<Message>() { message });
        else
            undeliveredMessages!.Add(message);
    }
    public void Remove(Guid messageId, string targetGroup)
    {
        if (string.IsNullOrWhiteSpace(targetGroup))
            return;

        bool keyExists = userUndeliveredMessagesKV.TryGetValue(targetGroup, out List<Message>? undeliveredMessages);

        if (!keyExists)
            throw new ApplicationException($"{userUndeliveredMessagesKV} has no such key.");

        if (keyExists)
        {
            Message? message = undeliveredMessages?.FirstOrDefault(x=>x.Id == messageId);
            if (message != null)
                undeliveredMessages?.Remove(message);
        }
    }
}
