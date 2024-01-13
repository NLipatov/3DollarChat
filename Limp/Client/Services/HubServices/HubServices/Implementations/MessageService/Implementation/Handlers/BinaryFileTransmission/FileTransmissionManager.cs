using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Services.DataTransmission.PackageForming;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryFileTransmission;

public class FileTransmissionManager : IFileTransmissionManager
{
    private ConcurrentDictionary<Guid, (int chunksLoaded, int chunksTotal)> FileIdUploadProgress = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly IMessageBox _messageBox;
    private readonly ICallbackExecutor _callbackExecutor;
    private readonly IPackageMultiplexerService _packageMultiplexerService;
    private readonly ICryptographyService _cryptographyService;

    public FileTransmissionManager
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
                $"Exception:{nameof(FileTransmissionManager)}.{nameof(StoreMetadata)}: Invalid metadata message.");

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
                $"Exception:{nameof(FileTransmissionManager)}.{nameof(SendMetadata)}: Invalid metadata.");

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

        await _messageBox.AddMessageAsync(metadataMessage, false);

        await SendViaHubConnectionAsync(new Message()
        {
            Type = metadataMessage.Type,
            Sender = metadataMessage.Sender,
            TargetGroup = metadataMessage.TargetGroup,
            Metadata = metadataMessage.Metadata,
        }, getHubConnection);

        await AddBinaryAsBlobToMessageBox(metadataMessage.Metadata, message.BrowserFile, message.Sender,
            message.TargetGroup);

        int chunksCounter = 0;
        await foreach (var chunk in chunkableBinary.GenerateChunksAsync())
        {
            var package = new ClientPackage()
            {
                Index = chunksCounter,
                PlainB64Data = chunk,
                FileDataid = fileDataId
            };

            var packageMessage = new Message
            {
                Id = fileDataId,
                Package = await EncryptPackage(package, message.TargetGroup),
                Sender = message.Sender,
                TargetGroup = message.TargetGroup,
                Type = MessageType.DataPackage,
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
            TargetGroup = message.TargetGroup
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
            
            await _messageBox.AddMessageAsync(new ClientMessage()
            {
                BlobLink = blobUrl,
                Id = metadata.DataFileId,
                Type = MessageType.BlobLink,
                TargetGroup = receiver,
                Sender = sender,
                Metadata = metadata
            }, isEncrypted: false);
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
            Index = package.Index
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