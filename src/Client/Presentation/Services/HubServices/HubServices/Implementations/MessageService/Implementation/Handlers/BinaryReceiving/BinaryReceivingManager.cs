using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;

public class BinaryReceivingManager : IBinaryReceivingManager
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IMessageBox _messageBox;
    private ConcurrentDictionary<Guid, Metadata> FileIdToMetadata = new();
    private ConcurrentDictionary<Guid, Package[]> FileIdToPackages = new();

    public BinaryReceivingManager(IJSRuntime jsRuntime, IMessageBox messageBox)
    {
        _jsRuntime = jsRuntime;
        _messageBox = messageBox;
    }

    public async Task<(bool, Guid)> StoreAsync(Package message)
    {
        (bool isLoadingCompleted, Guid fileId) progressStatus = Store(message);

        if (progressStatus.isLoadingCompleted)
            await AddToMessageBoxAsync(message);

        return progressStatus;
    }

    public (bool, Guid) Store(Metadata metadata)
    {
        FileIdToMetadata.TryAdd(metadata.DataFileId, metadata);

        return (IsLoadingCompleted(metadata.DataFileId), metadata.DataFileId);
    }

    public (bool, Guid) Store(Package package)
    {
        if (!FileIdToPackages.ContainsKey(package.FileDataid))
        {
            FileIdToPackages.TryAdd(package.FileDataid, new Package[package.Total]);
        }

        FileIdToPackages.AddOrUpdate(package.FileDataid,
            _ => [package],
            (_, existingData) =>
            {
                existingData[package.Index] = package;
                return existingData;
            });

        return (IsLoadingCompleted(package.FileDataid), package.FileDataid);
    }

    private bool IsLoadingCompleted(Guid fileId)
    {
        FileIdToPackages.TryGetValue(fileId, out var packages);
        
        if (packages?.FirstOrDefault()?.Total == packages?.Where(x=>x is not null).Count())
            return true;

        return false;
    }

    private async Task AddToMessageBoxAsync(Package package)
    {
        var packages = PopData(package.FileDataid);
        var metadata = new Metadata
        {
            Filename = package.Filename,
            ChunksCount = package.Total,
            ContentType = package.ContentType,
            DataFileId = package.FileDataid
        };
        var data = packages
            .SelectMany(x => x.Data)
            .ToArray();

        var blobUrl = await _jsRuntime.InvokeAsync<string>("createBlobUrl", data, metadata!.ContentType);

        _messageBox.AddMessage(new ClientMessage
        {
            BlobLink = blobUrl,
            Id = metadata.DataFileId,
            Type = MessageType.BlobLink,
            Target = package.Target,
            Sender = package.Sender,
            Metadata = metadata
        });
    }

    public Package[] PopData(Guid fileId)
    {
        FileIdToPackages.TryGetValue(fileId, out var data);
        FileIdToPackages.TryRemove(fileId, out var _);

        return data ?? [];
    }

    public Metadata PopMetadata(Guid fileId)
    {
        FileIdToMetadata.TryGetValue(fileId, out var metadata);
        FileIdToMetadata.TryRemove(fileId, out var _);
        return metadata;
    }
}