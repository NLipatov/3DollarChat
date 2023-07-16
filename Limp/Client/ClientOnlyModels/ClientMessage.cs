using LimpShared.Models.Message;
using System.Text.Json;

namespace Limp.Client.ClientOnlyModels
{
    public class ClientMessage : Message
    {
        public string PlainText { get; set; } = string.Empty;
        public bool IsUserNotified { get; set; } = false;
    }
}
