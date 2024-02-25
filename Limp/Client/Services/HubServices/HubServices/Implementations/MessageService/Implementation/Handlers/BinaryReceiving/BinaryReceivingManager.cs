using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using EthachatShared.Models.Message.TransferStatus;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;

public class BinaryReceivingManager : IBinaryReceivingManager
{
    private readonly ICryptographyService _cryptographyService;
    private readonly IJSRuntime _jsRuntime;
    private readonly IMessageBox _messageBox;
    private ConcurrentDictionary<Guid, Metadata> FileIdToMetadata = new();
    private ConcurrentDictionary<Guid, ClientPackage[]> FileIdToPackages = new();

    public BinaryReceivingManager(ICryptographyService cryptographyService, IJSRuntime jsRuntime,
        IMessageBox messageBox)
    {
        _cryptographyService = cryptographyService;
        _jsRuntime = jsRuntime;
        _messageBox = messageBox;
    }

    public async Task<(bool, Guid)> StoreAsync(Message message)
    {
        (bool isLoadingCompleted, Guid fileId) progressStatus = message.Type switch
        {
            MessageType.Metadata => Store(message.Metadata ?? throw new ArgumentException("Invalid metadata")),
            MessageType.DataPackage => Store(await GetDecryptedPackage(message)),
            _ => throw new ArgumentException("Unhandled type passed in")
        };

        if (progressStatus.isLoadingCompleted)
            await AddToMessageBoxAsync(message);

        return progressStatus;
    }

    private async Task<ClientPackage> GetDecryptedPackage(Message message)
    {
        var decryptedB64 = await _cryptographyService.DecryptAsync<AESHandler>(new()
        {
            Cyphertext = message.Package?.B64Data ??
                         throw new ArgumentException("Cypher text cannot be an empty string."),
            Iv = message.Package.IV
        }, message.Sender);

        return new ClientPackage()
        {
            Index = message.Package.Index,
            Total = message.Package.Total,
            PlainB64Data = decryptedB64.Cyphertext ??
                           throw new ApplicationException("Plain Base64 data cannot be an empty string."),
            FileDataid = message.Package.FileDataid
        };
    }

    public (bool, Guid) Store(Metadata metadata)
    {
        FileIdToMetadata.TryAdd(metadata.DataFileId, metadata);

        return (IsLoadingCompleted(metadata.DataFileId), metadata.DataFileId);
    }

    public (bool, Guid) Store(ClientPackage package)
    {
        if (!FileIdToPackages.ContainsKey(package.FileDataid))
        {
            FileIdToPackages.TryAdd(package.FileDataid, new ClientPackage[package.Total]);
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
            .SelectMany(x => Convert.FromBase64String(x.PlainB64Data))
            .ToArray();

        var blobUrl = await _jsRuntime.InvokeAsync<string>("createBlobUrl", data, metadata!.ContentType);

        _messageBox.AddMessage(new ClientMessage()
        {
            BlobLink = blobUrl,
            Id = metadata.DataFileId,
            Type = MessageType.BlobLink,
            TargetGroup = message.TargetGroup,
            Sender = message.Sender,
            Metadata = metadata,
            DateSent = message.DateSent
        });
    }

    public ClientPackage[] PopData(Guid fileId)
    {
        FileIdToPackages.TryGetValue(fileId, out var data);
        FileIdToPackages.TryRemove(fileId, out var _);

        return data ?? Array.Empty<ClientPackage>();
    }

    public Metadata PopMetadata(Guid fileId)
    {
        FileIdToMetadata.TryGetValue(fileId, out var metadata);
        FileIdToMetadata.TryRemove(fileId, out var _);
        return metadata;
    }
}