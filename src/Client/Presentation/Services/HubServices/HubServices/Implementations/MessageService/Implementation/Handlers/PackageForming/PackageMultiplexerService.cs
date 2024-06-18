using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming.Models.TransmittedBinaryFileModels;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming;

class PackageMultiplexerService : IPackageMultiplexerService
{
    public async Task<ChunkableBinary> SplitAsync(IBrowserFile file)
    {
        var memoryStream = new MemoryStream();

        await file
            .OpenReadStream(long.MaxValue)
            .CopyToAsync(memoryStream);
        
        memoryStream.Seek(0, SeekOrigin.Begin);

        return new ChunkableBinary(memoryStream);
    }
}