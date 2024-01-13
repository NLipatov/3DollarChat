using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.ClientOnlyModels
{
    public class ClientMessage : Message
    {
        public string PlainText { get; set; } = string.Empty;
        public bool IsToastShown { get; set; } = false;
        public List<ClientPackage> Packages { get; set; } = new();
        public List<ClientDataFile> ClientFiles { get; set; }
        public List<DataFile> Files { get; set; } = new();
        public new required MessageType Type { get; set; }
    }
}
