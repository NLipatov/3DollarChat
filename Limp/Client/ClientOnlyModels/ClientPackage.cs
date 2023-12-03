using LimpShared.Models.Message.DataTransfer;

namespace Limp.Client.ClientOnlyModels;

public record ClientPackage : Package
{
    public string PlainB64Data { get; set; }
}