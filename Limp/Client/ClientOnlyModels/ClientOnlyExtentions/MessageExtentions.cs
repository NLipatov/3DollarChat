using LimpShared.Models.Message;
using System.Text.Json;

namespace Ethachat.Client.ClientOnlyModels.ClientOnlyExtentions
{
    public static class MessageExtentions
    {
        public static ClientMessage AsClientMessage(this Message message)
        {
            ClientMessage? decrypted = JsonSerializer.Deserialize<ClientMessage>(JsonSerializer.Serialize(message));
            if (decrypted is null)
                throw new ArgumentException($"Could not convert given {typeof(Message).Name} to {typeof(ClientMessage).Name}.");

            return decrypted;
        }
    }
}
