namespace Ethachat.Client.Cryptography.Exceptions;

public class EncryptionKeyNotFoundException : Exception
{
    public EncryptionKeyNotFoundException(string message) : base(message) { }
}