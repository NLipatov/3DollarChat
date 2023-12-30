using LimpShared.Models.Message.DataTransfer;

namespace Ethachat.Client.ClientOnlyModels;

public record ClientPackage : Package
{
    public string PlainB64Data { get; set; }
}