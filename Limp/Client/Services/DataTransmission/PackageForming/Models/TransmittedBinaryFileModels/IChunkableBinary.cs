namespace Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;

public interface IChunkableBinary
{
    /// <summary>
    /// Returns a total number of chunks
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets next chunk of Base64 data
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<string> GenerateChunksAsync();
}