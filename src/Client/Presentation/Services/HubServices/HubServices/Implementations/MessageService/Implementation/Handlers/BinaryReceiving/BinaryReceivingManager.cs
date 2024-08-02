using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;

public class BinaryReceivingManager(IJSRuntime jsRuntime, IMessageBox messageBox) : IBinaryReceivingManager
{
    private readonly ConcurrentDictionary<Guid, Package[]> _fileIdToPackages = new();

    public async Task<(bool, Guid)> StoreAsync(Package message)
    {
        (bool isLoadingCompleted, Guid fileId) progressStatus = Store(message);

        if (progressStatus.isLoadingCompleted)
            await AddToMessageBoxAsync(message);

        return progressStatus;
    }

    private (bool, Guid) Store(Package package)
    {
        if (!_fileIdToPackages.ContainsKey(package.FileDataid))
        {
            _fileIdToPackages.TryAdd(package.FileDataid, new Package[package.Total]);
        }

        _fileIdToPackages.AddOrUpdate(package.FileDataid,
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
        _fileIdToPackages.TryGetValue(fileId, out var packages);

        return packages?.All(x=>x.Data is not null) ?? false;
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

        var blobUrl = await jsRuntime.InvokeAsync<string>("createBlobUrl", data, metadata.ContentType);

        messageBox.AddMessage(new ClientMessage
        {
            BlobLink = blobUrl,
            Id = metadata.DataFileId,
            Type = MessageType.BlobLink,
            Target = package.Target,
            Sender = package.Sender,
            Metadata = metadata
        });
    }

    private Package[] PopData(Guid fileId)
    {
        _fileIdToPackages.TryRemove(fileId, out var data);

        return data ?? [];
    }
}