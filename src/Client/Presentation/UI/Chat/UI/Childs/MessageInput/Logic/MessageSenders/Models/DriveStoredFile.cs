namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.Models;

internal record DriveStoredFile
{
    public Guid Id { get; set; }
    public required string ContentType { get; set; }
    public required string Filename { get; set; }
}