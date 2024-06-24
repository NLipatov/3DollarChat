using System.Text;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using MessagePack;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.ClientOnlyModels
{
    public class ClientMessage : Message
    {
        private HashSet<int> _textChunkIndexes = new();
        private List<TextChunk> _textChunks = new();
        public string PlainText => GetText();
        public bool IsToastShown { get; set; } = false;
        public List<Package> Packages { get; set; } = new();
        public List<ClientDataFile> ClientFiles { get; set; }
        public List<DataFile> Files { get; set; } = new();
        public new required MessageType Type { get; set; }
        public IBrowserFile BrowserFile { get; set; }

        public void AddChunk(TextChunk chunk)
        {
            if (!_textChunkIndexes.Contains(chunk.Index))
            {
                _textChunks.Add(chunk);
                _textChunkIndexes.Add(chunk.Index);
            }
        }

        public string GetText()
        {
            var sb = new StringBuilder();
            foreach (var chunk in _textChunks.OrderBy(x=>x.Index))
            {
                sb.Append(chunk.Text);
            }

            return sb.ToString();
        }
    }

    [MessagePackObject]
    public class TextChunk
    {
        [Key(0)] public int Index { get; set; }
        [Key(1)] public int Total { get; set; }
        [Key(2)] public string Text { get; set; } = string.Empty;
    }
}
