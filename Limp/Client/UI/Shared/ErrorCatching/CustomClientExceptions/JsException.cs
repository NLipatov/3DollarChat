namespace Ethachat.Client.Shared.ErrorCatching.CustomClientExceptions;

public class JsException : Exception
{
    public JsException(string message, string scriptName) : base(message)
    {
        Source = scriptName;
    }
}