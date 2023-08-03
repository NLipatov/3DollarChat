﻿using LimpShared.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace Limp.Server.Hubs.MessageDispatcher.Helpers.MessageSender
{
    public interface IMessageSendHandler
    {
        Task MarkAsReceived(Guid messageId, string topicName, IHubCallerClients clients);
        Task SendAsync(Message message, IHubCallerClients clients);
        Task MarkAsReaded(Guid messageId, string messageSender, IHubCallerClients clients);
    }
}