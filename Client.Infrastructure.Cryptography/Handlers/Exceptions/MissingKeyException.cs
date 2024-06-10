namespace Client.Infrastructure.Cryptography.Handlers.Exceptions;

internal class MissingKeyException : Exception
{
    public MissingKeyException() : base()
    {
    }

    public MissingKeyException(string message) : base(message)
    {
    }
}