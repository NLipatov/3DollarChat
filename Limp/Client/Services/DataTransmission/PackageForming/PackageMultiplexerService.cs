using Ethachat.Client.ClientOnlyModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.DataTransmission.PackageForming;

class PackageMultiplexerService : IPackageMultiplexerService
{
    public string Combine(List<ClientPackage> packages, string partnerUsername)
    {
        byte[] combinedBytes = packages
            .OrderBy(p => p.Index)
            .SelectMany(p => Convert.FromBase64String(p.PlainB64Data))
            .ToArray();

        return Convert.ToBase64String(combinedBytes);
    }

    public async Task<List<string>> SplitAsync(IBrowserFile file)
    {
        var bytes = await FileToBytesAsync(file);
        var base64String = Convert.ToBase64String(bytes);
        return SplitStringToChunks(base64String, 20 * 1024 /*KiB*/);
    }
    
    private async Task<byte[]> FileToBytesAsync(IBrowserFile file)
    {
        await using (var fileStream = file.OpenReadStream(long.MaxValue))
        {
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
    
    private List<string> SplitStringToChunks(string input, int maxChunkLength)
    {
        return Enumerable.Range(0, (int)Math.Ceiling((double)input.Length / maxChunkLength))
            .Select(i => new
            {
                Index = i, 
                Length = Math.Min(maxChunkLength, input.Length - i * maxChunkLength)
            })
            .Select(x => new string(input.ToCharArray(), x.Index * maxChunkLength, x.Length))
            .ToList();
    }
}