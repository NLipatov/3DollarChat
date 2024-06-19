﻿using EthachatShared.Models.Message.Interfaces;

namespace Ethachat.Server.Services.Notifications.WebPush
{
    public interface IWebPushNotificationService
    {
        Task SendAsync<T>(T itemToNotifyAbout)
            where T : ISourceResolvable, IDestinationResolvable;
    }
}