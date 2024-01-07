using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.DataTransmission.PackageForming.Models.TransmittedBinaryFileModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.DataTransmission.PackageForming;

class PackageMultiplexerService : IPackageMultiplexerService
{
    public async Task<ChunkableBinary> Split(IBrowserFile file)
    {
        var bytes = await FileToBytesAsync(file);
        return new ChunkableBinary(bytes);
    }

    public async Task CombineAsync(MemoryStream memoryStream, IEnumerable<ClientPackage> packages)
    {
        List<Task<byte[]>> tasks = packages
            .OrderBy(p => p.Index)
            .Select(p => Task.Run(() => Convert.FromBase64String(p.PlainB64Data)))
            .ToList();

        await Task.WhenAll(tasks);

        byte[] combinedBytes = tasks
            .Select(t => t.Result)
            .SelectMany(b => b)
            .ToArray();

        await memoryStream.WriteAsync(combinedBytes, 0, combinedBytes.Length);
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