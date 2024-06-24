namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming.Models.TransmittedBinaryFileModels
{
    /// <param name="Data">Data to create chunks from</param>
    /// <param name="MaxChunkSizeInKb"> 32Kb is a default per connection buffer value
    /// Here we use 31, so we have a 1KB margin for other data in message
    /// see https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-8.0</param>
    public record ChunkableBinary(byte[] Data, int MaxChunkSizeInKb = 31) : IChunkableBinary
    {
        public int Count => (int)Math.Ceiling((double)Data.Length / (MaxChunkSizeInKb * 1024));
        
        public IEnumerable<byte[]> GetChunk()
        {
            int maxChunkSizeInBytes = MaxChunkSizeInKb * 1024;
            int totalBytesRead = 0;

            while (totalBytesRead < Data.Length)
            {
                int bytesToRead = Math.Min(maxChunkSizeInBytes, Data.Length - totalBytesRead);
                byte[] chunk = new byte[bytesToRead];
                Array.Copy(Data, totalBytesRead, chunk, 0, bytesToRead);
                totalBytesRead += bytesToRead;
                yield return chunk;
            }
        }
    }
}