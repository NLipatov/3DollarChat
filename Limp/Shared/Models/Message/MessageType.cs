namespace ClientServerCommon.Models.Message;

public enum MessageType
{
    /// <summary>
    /// A message that user will see in GUI
    /// </summary>
    UserMessage,
    /// <summary>
    /// System message containing AES key proposal
    /// </summary>
    AESOffer,
    /// <summary>
    /// System message that is sent to accept AES key proposal
    /// </summary>
    AESAccept
}
