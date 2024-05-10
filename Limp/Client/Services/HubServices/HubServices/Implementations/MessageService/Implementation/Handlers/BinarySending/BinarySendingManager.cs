using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.PackageForming;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;

public class BinarySendingManager : IBinarySendingManager
{
    private ConcurrentDictionary<Guid, (int chunksLoaded, int chunksTotal)> FileIdUploadProgress = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly IMessageBox _messageBox;
    private readonly ICallbackExecutor _callbackExecutor;
    private readonly IPackageMultiplexerService _packageMultiplexerService;
    private readonly ICryptographyService _cryptographyService;

    public BinarySendingManager
    (IJSRuntime jsRuntime,
        IMessageBox messageBox,
        ICallbackExecutor callbackExecutor,
        IPackageMultiplexerService packageMultiplexerService,
        ICryptographyService cryptographyService)
    {
        _jsRuntime = jsRuntime;
        _messageBox = messageBox;
        _callbackExecutor = callbackExecutor;
        _packageMultiplexerService = packageMultiplexerService;
        _cryptographyService = cryptographyService;
    }

    public void StoreMetadata(Message metadataMessage)
    {
        if (metadataMessage.Metadata is null)
            throw new ArgumentException(
                $"Exception:{nameof(BinarySendingManager)}.{nameof(StoreMetadata)}: Invalid metadata message.");

        _messageBox.Messages.Add(new ClientMessage
        {
            Type = MessageType.Metadata,
            Metadata = new Metadata()
            {
                Filename = metadataMessage.Metadata.Filename,
                ChunksCount = metadataMessage.Metadata.ChunksCount,
                ContentType = metadataMessage.Metadata.ContentType,
                DataFileId = metadataMessage.Metadata.DataFileId
            }
        });
    }

    public async Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection)
    {
        if (message.Metadata is null)
            throw new ArgumentException(
                $"Exception:{nameof(BinarySendingManager)}.{nameof(SendMetadata)}: Invalid metadata.");

        var connection = await getHubConnection();

        await connection.SendAsync("Dispatch", message);
    }

    public async Task SendFile(ClientMessage message, Func<Task<HubConnection>> getHubConnection)
    {
        var fileDataId = Guid.NewGuid();
        var chunkableBinary = await _packageMultiplexerService.SplitAsync(message.BrowserFile);
        int totalChunks = chunkableBinary.Count;
        FileIdUploadProgress.TryAdd(fileDataId, (0, totalChunks));
        var metadataMessage = GenerateMetadataMessage(fileDataId, message, totalChunks);

        _messageBox.AddMessage(metadataMessage);

        await SendViaHubConnectionAsync(new Message()
        {
            Type = metadataMessage.Type,
            Sender = metadataMessage.Sender,
            Target = metadataMessage.Target,
            Metadata = metadataMessage.Metadata,
            DateSent = DateTime.UtcNow
        }, getHubConnection);

        await AddBinaryAsBlobToMessageBox(metadataMessage.Metadata, message.BrowserFile, message.Sender,
            message.Target);

        int chunksCounter = 0;
        await foreach (var chunk in chunkableBinary.GenerateChunksAsync())
        {
            var package = new ClientPackage()
            {
                Index = chunksCounter,
                Total = totalChunks,
                PlainB64Data = chunk,
                FileDataid = fileDataId
            };

            var packageMessage = new Message
            {
                Package = await EncryptPackage(package, message.Target),
                Sender = message.Sender,
                Target = message.Target,
                Type = MessageType.DataPackage,
                DateSent = DateTime.UtcNow
            };

            await SendViaHubConnectionAsync(packageMessage, getHubConnection);

            chunksCounter++;
            decimal progress = Math.Round(chunksCounter / (decimal)totalChunks * 100);
            _callbackExecutor.ExecuteSubscriptionsByName(progress, "OnFileEncryptionProgressChanged");
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
            
            _messageBox.AddMessage(new ClientMessage()
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
        return await _jsRuntime.InvokeAsync<string>("createBlobUrl", bytes, contentType);
    }

    private async Task SendViaHubConnectionAsync(Message message, Func<Task<HubConnection>> getHubConnection)
    {
        var connection = await getHubConnection();

        await connection.SendAsync("Dispatch", message);
    }

    private async Task<Package> EncryptPackage(ClientPackage package, string usernameAesKey)
    {
        var cryptogram = await _cryptographyService.EncryptAsync<AESHandler>(new()
        {
            Cyphertext = package.PlainB64Data
        }, usernameAesKey);

        return new()
        {
            B64Data = cryptogram.Cyphertext,
            IV = cryptogram.Iv,
            FileDataid = package.FileDataid,
            Index = package.Index,
            Total = package.Total
        };
    }

    public void HandlePackageRegisteredByHub(Guid fileId, int packageIndex)
    {
        _callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnChunkLoaded");
        
        FileIdUploadProgress.TryGetValue(fileId, out var currentProgress);
        
        FileIdUploadProgress
            .TryUpdate(fileId, (currentProgress.chunksLoaded + 1, currentProgress.chunksTotal),
                (currentProgress.chunksLoaded, currentProgress.chunksTotal));

        if (FileIdUploadProgress[fileId].chunksLoaded == FileIdUploadProgress[fileId].chunksTotal)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(fileId, "MessageRegisteredByHub");
            FileIdUploadProgress.TryRemove(fileId, out _);
        }
    }
}