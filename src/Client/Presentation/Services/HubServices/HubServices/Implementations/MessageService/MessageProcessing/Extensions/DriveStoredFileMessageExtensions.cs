using Client.Transfer.Domain.Entities.Messages;
using EthachatShared.Models.Message;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Extensions;

public static class DriveStoredFileMessageExtensions
{
    public static async Task<ClientMessage> ToClientMessage(this DriveStoredFileMessage message, byte[] data, IJSRuntime jsRuntime)
    {
        var blobUrl = await jsRuntime.InvokeAsync<string>("createBlobUrl", data, message.ContentType);
        return new ClientMessage
        {
            Id = message.Id,
            Sender = message.Sender,
            Target = message.Target,
            Type = MessageType.BlobLink,
            BlobLink = blobUrl,
            Metadata = new ()
            {
                Filename = message.Filename,
                ContentType = message.ContentType,
                DataFileId = message.Id,
                ChunksCount = 0,
            }
        };
    }
}