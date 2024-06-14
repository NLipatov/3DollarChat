using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    PackageForming;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;

public class BinarySendingManager(
    IJSRuntime jsRuntime,
    IMessageBox messageBox,
    ICallbackExecutor callbackExecutor,
    IPackageMultiplexerService packageMultiplexerService)
    : IBinarySendingManager
{
    private ConcurrentDictionary<Guid, (int chunksLoaded, int chunksTotal)> _fileIdUploadProgress = new();

    public async IAsyncEnumerable<ClientMessage> GetChunksToSendAsync(ClientMessage message)
    {
        var fileDataId = Guid.NewGuid();
        var chunkableBinary = await packageMultiplexerService.SplitAsync(message.BrowserFile);
        int totalChunks = chunkableBinary.Count;
        _fileIdUploadProgress.TryAdd(fileDataId, (0, totalChunks));
        var metadataMessage = GenerateMetadataMessage(fileDataId, message, totalChunks);

        messageBox.AddMessage(metadataMessage);

        yield return new ClientMessage
        {
            Type = MessageType.Metadata,
            Sender = metadataMessage.Sender,
            Target = metadataMessage.Target,
            Metadata = metadataMessage.Metadata,
            DateSent = DateTime.UtcNow
        };

        await AddBinaryAsBlobToMessageBox(metadataMessage.Metadata, message.BrowserFile, message.Sender,
            message.Target);

        int chunksCounter = 0;
        await foreach (var chunk in chunkableBinary.GenerateChunksAsync())
        {
            var package = new Package
            {
                Index = chunksCounter,
                Total = totalChunks,
                Data = chunk,
                FileDataid = fileDataId
            };

            var packageMessage = new ClientMessage
            {
                Package = package,
                Sender = message.Sender,
                Target = message.Target,
                Type = MessageType.DataPackage,
                DateSent = DateTime.UtcNow,
            };

            yield return packageMessage;

            chunksCounter++;
            decimal progress = Math.Round(chunksCounter / (decimal)totalChunks * 100);
            callbackExecutor.ExecuteSubscriptionsByName(progress, "OnFileEncryptionProgressChanged");
        }
    }

    private ClientMessage GenerateMetadataMessage(Guid fileDataId, ClientMessage message, int totalChunks)
    {
        var metadataMessage = new ClientMessage
        {
            Type = MessageType.Metadata,
            Metadata = new()
            {
                DataFileId = fileDataId,
                ContentType = message.BrowserFile.ContentType,
                Filename = message.BrowserFile.Name,
                ChunksCount = totalChunks
            },
            Sender = message.Sender,
            Target = message.Target
        };

        return metadataMessage;
    }

    private async Task AddBinaryAsBlobToMessageBox(Metadata metadata, IBrowserFile file, string sender, string receiver)
    {
        await using (var fileStream = file.OpenReadStream(long.MaxValue))
        {
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var blobUrl = await BytesToBlobUrl(memoryStream.ToArray(), metadata.ContentType);

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
    }

    private async Task<string> BytesToBlobUrl(byte[] bytes, string contentType)
    {
        return await jsRuntime.InvokeAsync<string>("createBlobUrl", bytes, contentType);
    }

    public void HandlePackageRegisteredByHub(Guid fileId, int packageIndex)
    {
        callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnChunkLoaded");

        _fileIdUploadProgress.TryGetValue(fileId, out var currentProgress);

        _fileIdUploadProgress
            .TryUpdate(fileId, (currentProgress.chunksLoaded + 1, currentProgress.chunksTotal),
                (currentProgress.chunksLoaded, currentProgress.chunksTotal));

        if (_fileIdUploadProgress[fileId].chunksLoaded == _fileIdUploadProgress[fileId].chunksTotal)
        {
            callbackExecutor.ExecuteSubscriptionsByName(fileId, "MessageRegisteredByHub");
            _fileIdUploadProgress.TryRemove(fileId, out _);
        }
    }
}