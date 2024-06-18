using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using EthachatShared.Models.Message.ClientToClientTransferData;

namespace Ethachat.Client.UI.Chat.Logic.MessageBuilder;

public class MessageBuilder : IMessageBuilder
{
    private const int MaxTextChunkLength = 512;
    private readonly IAuthenticationHandler _authenticationHandler;

    public MessageBuilder(IAuthenticationHandler authenticationHandler)
    {
        _authenticationHandler = authenticationHandler;
    }

    public async IAsyncEnumerable<TextMessage> BuildTextMessage(ClientMessage message)
    {
        var currentUserUsername = await _authenticationHandler.GetUsernameAsync();
        var messagesCount = (int)Math.Ceiling(message.PlainText.Length / (decimal)MaxTextChunkLength);
        for (int i = 0; i * MaxTextChunkLength < message.PlainText.Length; i++)
        {
            var textChunk = message.PlainText.Substring(i * MaxTextChunkLength,
                Math.Min(MaxTextChunkLength, message.PlainText.Length - i * MaxTextChunkLength));

            var messageToSend = new TextMessage
            {
                Id = message.Id,
                Total = messagesCount,
                Target = message.Target ?? throw new ArgumentException("Missing message target"),
                Sender = currentUserUsername,
                Index = i,
                Text = textChunk,
            };

            yield return messageToSend;
        }
    }
}