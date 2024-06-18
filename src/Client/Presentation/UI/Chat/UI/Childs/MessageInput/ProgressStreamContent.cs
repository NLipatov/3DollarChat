using System.Net;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput;

public delegate void ProgressHandler(long bytes, long currentBytes, long totalBytes);

public class ProgressStreamContent : StreamContent
{
    private const int DefaultBufferSize = 4096;
    public event ProgressHandler ProgressChanged = delegate { };
    private long _bytesCounter = 0;
    private long TotalBytes { get; init; }
    private Stream InnerStream { get; }
    private int BufferSize { get; }

    public ProgressStreamContent(Stream innerStream, long contentSize, int bufferSize = DefaultBufferSize) :
        base(innerStream, bufferSize)
    {
        TotalBytes = contentSize;
        InnerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        BufferSize = bufferSize > 0 ? bufferSize : throw new ArgumentOutOfRangeException(nameof(bufferSize));
    }

    private void ResetInnerStream()
    {
        if (InnerStream.Position != 0)
        {
            if (InnerStream.CanSeek)
            {
                InnerStream.Position = 0;
                _bytesCounter = 0;
            }
            else
                throw new InvalidOperationException("The inner stream has already been read!");
        }
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        ResetInnerStream();

        var buffer = new byte[BufferSize];
        var bytesRead = 0;
        while ((bytesRead = await InnerStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            stream.Write(buffer, 0, bytesRead);
            _bytesCounter += bytesRead;

            ProgressChanged(bytesRead, _bytesCounter, TotalBytes);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        var result = base.TryComputeLength(out length);
        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            InnerStream.Dispose();

        base.Dispose(disposing);
    }
}