using System.Collections.Concurrent;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming.Models.TransmittedBinaryFileModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;

public class BinarySendingManager(
    IJSRuntime jsRuntime,
    IMessageBox messageBox,
    ICallbackExecutor callbackExecutor)
    : IBinarySendingManager
{
    private readonly ConcurrentDictionary<Guid, (int chunksLoaded, int chunksTotal)> _fileIdUploadProgress = new();

    public async IAsyncEnumerable<Package> GetChunksToSendAsync(Package data)
    {
        var fileDataId = Guid.NewGuid();
        var chunkedBytes = new ChunkedBytes(data.Data);
        int totalChunks = chunkedBytes.Count;
        _fileIdUploadProgress.TryAdd(fileDataId, (0, totalChunks));
        var metadataMessage = GenerateMetadataMessage(fileDataId, data, totalChunks);

        messageBox.AddMessage(metadataMessage);

        await AddBinaryAsBlobToMessageBox(metadataMessage.Metadata!, data.Data, data.Sender,
            data.Target);

        int chunksCounter = 0;
        foreach (var chunk in chunkedBytes.GetChunk())
        {
            var package = new Package
            {
                Id = Guid.NewGuid(),
                Sender = data.Sender,
                Target = data.Target,
                Index = chunksCounter,
                Total = totalChunks,
                Data = chunk,
                FileDataid = fileDataId,
                Filename = data.Filename,
                ContentType = data.ContentType,
            };

            yield return package;

            chunksCounter++;
            decimal progress = Math.Round(chunksCounter / (decimal)totalChunks * 100);
            callbackExecutor.ExecuteSubscriptionsByName(progress, "OnFileEncryptionProgressChanged");
        }
    }

    private ClientMessage GenerateMetadataMessage(Guid fileDataId, Package message, int totalChunks)
    {
        var metadataMessage = new ClientMessage
        {
            Type = MessageType.Metadata,
            Metadata = new()
            {
                DataFileId = fileDataId,
                ContentType = message.ContentType,
                Filename = message.Filename,
                ChunksCount = totalChunks
            },
            Sender = message.Sender,
            Target = message.Target
        };

        return metadataMessage;
    }

    private async Task AddBinaryAsBlobToMessageBox(Metadata metadata, byte[] fileData, string sender, string receiver)
    {
        var blobUrl = await BytesToBlobUrl(fileData, metadata.ContentType);

        messageBox.AddMessage(new ClientMessage
        {
            BlobLink = blobUrl,
            Id = metadata.DataFileId,
            Type = MessageType.BlobLink,
            Target = receiver,
            Sender = sender,
            Metadata = metadata,
            DateSent = DateTime.UtcNow
        });
    }

    private async Task<string> BytesToBlobUrl(byte[] bytes, string contentType)
    {
        return await jsRuntime.InvokeAsync<string>("createBlobUrl", bytes, contentType);
    }
}