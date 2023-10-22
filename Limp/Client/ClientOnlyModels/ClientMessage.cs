using LimpShared.Models.Message;
using LimpShared.Models.Message.DataTransfer;

namespace Limp.Client.ClientOnlyModels
{
    public class ClientMessage : Message
    {
        public string PlainText { get; set; } = string.Empty;
        public bool IsToastShown { get; set; } = false;
        public List<Package> Packages { get; set; } = new();
        public List<DataFile> Files { get; set; } = new();
    }
}
