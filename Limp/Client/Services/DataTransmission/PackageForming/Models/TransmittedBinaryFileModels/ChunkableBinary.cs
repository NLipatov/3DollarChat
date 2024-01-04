namespace Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;

public record ChunkableBinary(byte[] Bytes, int MaxChunkSizeInKb = 15) : IChunkableBinary
{
    public string Base64 => Convert.ToBase64String(Bytes);
    public int Count => (int)Math.Ceiling((double)Bytes.Length / (MaxChunkSizeInKb * 1024));
    public IEnumerator<string> GetEnumerator()
    {
        int maxChunkSizeInBytes = MaxChunkSizeInKb * 1024;

        for (int i = 0; i < Bytes.Length; i += maxChunkSizeInBytes)
        {
            int chunkSize = Math.Min(maxChunkSizeInBytes, Bytes.Length - i);
            byte[] chunk = new byte[chunkSize];
            Array.Copy(Bytes, i, chunk, 0, chunkSize);
            yield return Convert.ToBase64String(chunk);
        }
    }
}