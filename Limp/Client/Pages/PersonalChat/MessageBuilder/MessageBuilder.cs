﻿using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;

namespace Limp.Client.Pages.PersonalChat.MessageBuilder
{
    public class MessageBuilder : IMessageBuilder
    {
        private readonly ICryptographyService _cryptographyService;

        public MessageBuilder(ICryptographyService cryptographyService)
        {
            _cryptographyService = cryptographyService;
        }
        public async Task<Message> BuildMessageToBeSend(string plainMessageText, string topicName, string myName)
        {
            Cryptogramm cryptogramm = await _cryptographyService
                .EncryptAsync<AESHandler>(new Cryptogramm() { Cyphertext = plainMessageText }, contact: topicName);

            Message messageToSend = new Message
            {
                Topic = topicName,
                Cryptogramm = cryptogramm,
                DateSent = DateTime.UtcNow,
                TargetGroup = topicName!,
                Sender = myName ?? throw new ApplicationException("Message was not send"),
            };

            return messageToSend;
        }
    }
}
