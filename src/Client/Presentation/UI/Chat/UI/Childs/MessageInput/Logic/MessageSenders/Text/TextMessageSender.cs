using Client.Transfer.Domain.TransferedEntities.Messages;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.Text;

public class TextMessageSender(
    IAuthenticationHandler authenticationHandler,
    IMessageService messageService,
    IMessageBox messageBox) : ITextMessageSender
{
    private const int MaxTextChunkLength = 512;

    public async Task SendTextMessageAsync(string message, string target)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            var textMessage = new TextMessage
            {
                Sender = await authenticationHandler.GetUsernameAsync(),
                Target = target,
                Text = message,
                Index = 0,
                Total = 1
            };

            await AddToMessageBox(textMessage.Text, textMessage.Target, textMessage.Id);
            await foreach (var tMessage in BuildTextMessageAsync(textMessage))
                await messageService.TransferAsync(tMessage);
        }
    }

    private async IAsyncEnumerable<TextMessage> BuildTextMessageAsync(TextMessage message)
    {
        var messagesCount = (int)Math.Ceiling(message.Text.Length / (decimal)MaxTextChunkLength);
        for (int i = 0; i * MaxTextChunkLength < message.Text.Length; i++)
        {
            var textChunk = message.Text.Substring(i * MaxTextChunkLength,
                Math.Min(MaxTextChunkLength, message.Text.Length - i * MaxTextChunkLength));

            var messageToSend = new TextMessage
            {
                Id = message.Id,
                Total = messagesCount,
                Target = message.Target ?? throw new ArgumentException("Missing message target"),
                Sender = message.Sender,
                Index = i,
                Text = textChunk,
            };

            await Task.Yield();
            yield return messageToSend;
        }
    }

    private async Task AddToMessageBox(string plainText, string target, Guid id)
    {
        var message = new ClientMessage
        {
            Id = id,
            Sender = await authenticationHandler.GetUsernameAsync(),
            Target = target,
            DateSent = DateTime.UtcNow,
            Type = MessageType.TextMessage
        };
        message.AddChunk(new()
        {
            Index = 0,
            Total = 1,
            Text = plainText
        });
        messageBox.AddMessage(message);
    }
}