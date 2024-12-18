namespace Ethachat.Client.UI.ErrorBoundary.CustomClientExceptions;

public class JsException : Exception
{
    public JsException(string message, string scriptName) : base(message)
    {
        Source = scriptName;
    }
}