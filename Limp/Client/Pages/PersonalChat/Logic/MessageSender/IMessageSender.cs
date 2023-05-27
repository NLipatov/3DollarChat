namespace Limp.Client.Pages.PersonalChat.Logic.MessageSender
{
    public interface IMessageSender
    {
        Task SendMessageAsync(string text, string targetGroup, string myUsername);
    }
}