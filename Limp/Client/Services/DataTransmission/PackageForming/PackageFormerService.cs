using LimpShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components.Forms;

namespace Limp.Client.Services.DataTransmission.PackageForming;

class PackageFormerService : IPackageFormerService
{
    public async Task<List<Package>> Split(IBrowserFile file, int maxPackageSizeBytes, Guid fileDataId)
    {
        var bytes = await FileToBytes(file);
        
        int totalPackets = (int)Math.Ceiling((double)bytes.Length / maxPackageSizeBytes);

        List<Package> packages = new List<Package>();

        for (int i = 0; i < bytes.Length; i += maxPackageSizeBytes)
        {
            int size = Math.Min(maxPackageSizeBytes, bytes.Length - i);
            byte[] cur = new byte[size];
            Array.Copy(bytes, i, cur, 0, size);

            packages.Add(new Package
            {
                FileDataid = fileDataId,
                FileName = file.Name,
                ContentType = file.ContentType,
                Index = packages.Count,
                Total = totalPackets,
                B64Data = Convert.ToBase64String(cur)
            });
        }

        return packages;
    }

    private async Task<byte[]> FileToBytes(IBrowserFile file)
    {
        using (var s = file.OpenReadStream(500000000 ))
        {
            using (var ms = new MemoryStream())
            {
                await s.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }

    public string Combine(List<Package> packages)
    {
        byte[] combinedBytes = packages
            .OrderBy(p => p.Index)
            .SelectMany(p => Convert.FromBase64String(p.B64Data))
            .ToArray();

        string originalB64 = Convert.ToBase64String(combinedBytes);

        return originalB64;
    }
}