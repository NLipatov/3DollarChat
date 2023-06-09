﻿using ClientServerCommon.Models.Message;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using System;

namespace Limp.Client.Pages.PersonalChat.Logic.MessageBuilder
{
    public class MessageBuilder : IMessageBuilder
    {
        private readonly ICryptographyService _cryptographyService;

        public MessageBuilder(ICryptographyService cryptographyService)
        {
            _cryptographyService = cryptographyService;
        }
        public async Task<Message> BuildMessageToBeSend(string plainMessageText, string topicName, string myName, Guid id)
        {
            Cryptogramm cryptogramm = await _cryptographyService
                .EncryptAsync<AESHandler>(new Cryptogramm() { Cyphertext = plainMessageText }, contact: topicName);

            Message messageToSend = new Message
            {
                Id = id,
                Topic = topicName,
                Cryptogramm = cryptogramm,
                DateSent = DateTime.UtcNow,
                TargetGroup = topicName!,
                Sender = myName ?? throw new ApplicationException
                    ($"Exception on message building phase: Cannot define message sender name."),
            };

            return messageToSend;
        }
    }
}
