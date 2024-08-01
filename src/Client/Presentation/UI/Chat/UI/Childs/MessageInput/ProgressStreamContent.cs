using System.Net;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput;

public delegate void ProgressHandler(long bytes, long currentBytes, long totalBytes);

public class ProgressStreamContent(
    Stream innerStream,
    long contentSize,
    int bufferSize = ProgressStreamContent.DefaultBufferSize)
    : StreamContent(innerStream, bufferSize)
{
    private const int DefaultBufferSize = 4096;
    public event ProgressHandler ProgressChanged = delegate { };
    private long _bytesCounter;
    private long TotalBytes { get; } = contentSize;
    private Stream InnerStream { get; } = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
    private int BufferSize { get; } = bufferSize > 0 ? bufferSize : throw new ArgumentOutOfRangeException(nameof(bufferSize));

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

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        ResetInnerStream();

        var buffer = new byte[BufferSize];
        int bytesRead;
        while ((bytesRead = await InnerStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
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