namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.Text;

public interface ITextMessageSender
{
    Task SendTextMessageAsync(string message, string target);
}