using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.BrowserFile;

public interface IBrowserFileSender
{
    Task SendIBrowserFile(IBrowserFile browserFile, string target);
}