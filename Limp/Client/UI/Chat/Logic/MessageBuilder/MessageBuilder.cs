using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.TransferStatus;

namespace Ethachat.Client.UI.Chat.Logic.MessageBuilder
{
    public class MessageBuilder : IMessageBuilder
    {
        private const int MaxTextChunkLength = 512;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAuthenticationHandler _authenticationHandler;

        public MessageBuilder(ICryptographyService cryptographyService, IAuthenticationHandler authenticationHandler)
        {
            _cryptographyService = cryptographyService;
            _authenticationHandler = authenticationHandler;
        }
        
        public async IAsyncEnumerable<Message> BuildTextMessageToBeSend(ClientMessage message)
        {
            var currentUserUsername = await _authenticationHandler.GetUsernameAsync();
            var messagesCount = (int)Math.Ceiling(message.PlainText.Length / (decimal)MaxTextChunkLength);
            for (int i = 0; i * MaxTextChunkLength < message.PlainText.Length; i++)
            {
                var textChunk = message.PlainText.Substring(i * MaxTextChunkLength, Math.Min(MaxTextChunkLength, message.PlainText.Length - i * MaxTextChunkLength));
                Cryptogramm cryptogramm = await _cryptographyService
                    .EncryptAsync<AESHandler>(new Cryptogramm
                    {
                        Cyphertext = textChunk,
                    }, contact: message.TargetGroup);

                Message messageToSend = new Message
                {
                    SyncItem = new SyncItem
                    {
                        Index = i,
                        TotalItems = messagesCount,
                        MessageId = message.Id
                    },
                    Type = MessageType.TextMessage,
                    Id = Guid.NewGuid(),
                    Cryptogramm = cryptogramm,
                    DateSent = DateTime.UtcNow,
                    TargetGroup = message.TargetGroup,
                    Sender = currentUserUsername ?? throw new ApplicationException
                        ($"Exception on message building phase: Cannot define message sender name."),
                };

                yield return messageToSend;
            }
        }
    }
}
