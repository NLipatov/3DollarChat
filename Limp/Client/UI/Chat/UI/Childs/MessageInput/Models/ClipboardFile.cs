using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Models;

public class ClipboardFile : IBrowserFile
{
    private readonly byte[] _content;
    private readonly string _name;
    private readonly string _contentType;

    public ClipboardFile(byte[] content, string name, string contentType)
    {
        _content = content;
        _name = name;
        _contentType = contentType;
    }
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = new CancellationToken())
    {
        return new MemoryStream(_content);
    }

    public string Name => _name;
    public DateTimeOffset LastModified => DateTimeOffset.Now;
    public long Size => _content.Length;
    public string ContentType => _contentType;
}