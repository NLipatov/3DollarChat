namespace Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;

public record ChunkableBinary(MemoryStream DataStream, int MaxChunkSizeInKb = 15) : IChunkableBinary
{
    public int Count => (int)Math.Ceiling((double)DataStream.Length / (MaxChunkSizeInKb * 1024));
    public async IAsyncEnumerable<string> GenerateChunksAsync()
    {
        int maxChunkSizeInBytes = MaxChunkSizeInKb * 1024;
        byte[] buffer = new byte[maxChunkSizeInBytes];

        int bytesRead;
        long totalBytesRead = 0;

        while ((bytesRead = await DataStream.ReadAsync(buffer, 0, maxChunkSizeInBytes)) > 0)
        {
            totalBytesRead += bytesRead;
            yield return Convert.ToBase64String(buffer, 0, bytesRead);
        }
    }
}