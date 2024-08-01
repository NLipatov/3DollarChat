using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Strategies.SendStrategies;

public class OnSentTextMessage(
    IMessageService messageService,
    IAuthenticationHandler authenticationHandler,
    IMessageBox messageBox) : ITransferHandler<TextMessage>
{
    private const int MaxTextChunkLength = 512;

    public async Task HandleAsync(TextMessage textMessage)
    {
        await AddToMessageBox(textMessage.Text, textMessage.Target, textMessage.Id);
        await foreach (var tMessage in BuildTextMessageAsync(textMessage))
            await messageService.TransferAsync(tMessage);
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
}