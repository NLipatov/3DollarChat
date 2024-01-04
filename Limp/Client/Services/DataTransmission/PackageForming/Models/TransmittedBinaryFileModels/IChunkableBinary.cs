namespace Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;

public interface IChunkableBinary
{
    /// <summary>
    /// Returns a Base64 string representing unchunked file
    /// </summary>
    string Base64 { get; }
    
    /// <summary>
    /// Returns a total number of chunks
    /// </summary>
    int Count { get; }
    
    /// <summary>
    /// Gets next chunk of Base64 data
    /// </summary>
    /// <returns></returns>
    IEnumerator<string> GetEnumerator();
}