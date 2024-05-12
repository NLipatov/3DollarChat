namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming.Models.TransmittedBinaryFileModels;

public record ChunkableBinary(MemoryStream DataStream, int MaxChunkSizeInKb = 15) : IChunkableBinary
{
    public int Count => (int)Math.Ceiling((double)DataStream.Length / (MaxChunkSizeInKb * 1024));
    public async IAsyncEnumerable<byte[]> GenerateChunksAsync()
    {
        int maxChunkSizeInBytes = MaxChunkSizeInKb * 1024;
        byte[] buffer = new byte[maxChunkSizeInBytes];

        int bytesRead;
        long totalBytesRead = 0;

        while ((bytesRead = await DataStream.ReadAsync(buffer, 0, maxChunkSizeInBytes)) > 0)
        {
            totalBytesRead += bytesRead;
            byte[] chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            yield return chunk;
        }
    }
}