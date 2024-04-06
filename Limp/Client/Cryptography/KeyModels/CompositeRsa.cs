#nullable disable
namespace Ethachat.Client.Cryptography.KeyModels;

public record CompositeRsa
{
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
}