namespace Client.Infrastructure.Cryptography.Handlers.Models;

internal class EncryptionResult
{
    public string Ciphertext { get; set; } = string.Empty;
    public string Iv { get; set; } = string.Empty;
}