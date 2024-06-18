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

    public async Task<(bool, Guid)> StoreAsync(ClientMessage message)
    {
        (bool isLoadingCompleted, Guid fileId) progressStatus = message.Type switch
        {
            MessageType.Metadata => Store(message.Metadata ?? throw new ArgumentException("Invalid metadata")),
            MessageType.DataPackage => Store(new Package()
        {
            Index = message.Package.Index,
            Total = message.Package.Total,
            Data = message.Package.Data,
            FileDataid = message.Package.FileDataid
        }),
            _ => throw new ArgumentException($"Unhandled type passed in - {message.Type}")
        };

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
        FileIdToMetadata.TryGetValue(fileId, out var metadata);

        FileIdToPackages.TryGetValue(fileId, out var packages);
        
        var packagesCount = packages?.Count(x => x is not null) ?? 0;
        var totalCount = metadata?.ChunksCount ?? 0;

        return packagesCount == totalCount && packagesCount > 0;
    }

    private async Task AddToMessageBoxAsync(Message message)
    {
        var metadata = PopMetadata(message.Package.FileDataid);
        var packages = PopData(message.Package.FileDataid);
        var data = packages
            .SelectMany(x => x.Data)
            .ToArray();

        var blobUrl = await _jsRuntime.InvokeAsync<string>("createBlobUrl", data, metadata!.ContentType);

        _messageBox.AddMessage(new ClientMessage()
        {
            BlobLink = blobUrl,
            Id = metadata.DataFileId,
            Type = MessageType.BlobLink,
            Target = message.Target,
            Sender = message.Sender,
            Metadata = metadata,
            DateSent = message.DateSent
        });
    }

    public Package[] PopData(Guid fileId)
    {
        FileIdToPackages.TryGetValue(fileId, out var data);
        FileIdToPackages.TryRemove(fileId, out var _);

        return data ?? Array.Empty<Package>();
    }

    public Metadata PopMetadata(Guid fileId)
    {
        FileIdToMetadata.TryGetValue(fileId, out var metadata);
        FileIdToMetadata.TryRemove(fileId, out var _);
        return metadata;
    }
}