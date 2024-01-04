namespace Ethachat.Client.Services.DataTransmission.PackageForming.Models;

public record ByteChunks(byte[] bytes, int maxChunkSizeInKB = 15)
{
    public int Count => (int)Math.Ceiling((double)bytes.Length / (maxChunkSizeInKB * 1024));
    public int ChunkCount()
    {
        int maxChunkSizeInBytes = maxChunkSizeInKB * 1024;
        int count = 0;

        for (int i = 0; i < bytes.Length; i += maxChunkSizeInBytes)
        {
            count++;
        }

        return count;
    }
    public IEnumerator<string> GetEnumerator()
    {
        int maxChunkSizeInBytes = maxChunkSizeInKB * 1024;

        for (int i = 0; i < bytes.Length; i += maxChunkSizeInBytes)
        {
            int chunkSize = Math.Min(maxChunkSizeInBytes, bytes.Length - i);
            byte[] chunk = new byte[chunkSize];
            Array.Copy(bytes, i, chunk, 0, chunkSize);
            yield return Convert.ToBase64String(chunk);
        }
    }
}