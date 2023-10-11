using LimpShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components.Forms;

namespace Limp.Client.Services.DataTransmission.PackageForming;

public interface IPackageFormerService
{
    Task<List<Package>> Split(IBrowserFile file, int maxPackageSizeBytes, Guid fileDataId);
    string Combine(List<Package> packages);
}