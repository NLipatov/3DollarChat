using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryFileTransmission;

public class FileTransmissionManager : IFileTransmissionManager
{
    private ConcurrentDictionary<Guid, List<ClientPackage>> _downloadedFileIdToPackages = new();
    private ConcurrentDictionary<Guid, List<int>> _uploadedFileIdToPackages = new();
    private ConcurrentDictionary<Guid, int> _uploadedFilePackagesRemainTobeUploaded = new();
    private ConcurrentDictionary<Guid, int> UploadedPackages = new();
    private ConcurrentDictionary<Guid, int> DownloadedPackages = new();
    private ConcurrentDictionary<Guid, int> PackageCount = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly IMessageBox _messageBox;
    private readonly ICallbackExecutor _callbackExecutor;

    public FileTransmissionManager
        (IJSRuntime jsRuntime, 
        IMessageBox messageBox,
        ICallbackExecutor callbackExecutor)
    {
        _jsRuntime = jsRuntime;
        _messageBox = messageBox;
        _callbackExecutor = callbackExecutor;
    }

    public async Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection)
    {
        if (message.Metadata is null)
            throw new ArgumentException($"Exception:{nameof(FileTransmissionManager)}.{nameof(SendMetadata)}: Invalid metadata.");
        
        var connection = await getHubConnection();

        await connection.SendAsync("Dispatch", message);
    }

    public async Task SendDataPackage(Guid fileId, Package package, ClientMessage messageToSend, Func<Task<HubConnection>> getHubConnection)
    {
        var keyExist = _uploadedFileIdToPackages.ContainsKey(fileId);
        if (!keyExist)
            _uploadedFileIdToPackages.TryAdd(fileId, new List<int>());
        
        var oldIndexes = _uploadedFileIdToPackages[fileId];
        oldIndexes.Add(package.Index);
        
        _uploadedFileIdToPackages.TryUpdate(fileId, oldIndexes, _uploadedFileIdToPackages[fileId]);
        var message = new Message
        {
            Id = fileId,
            Package = package,
            Sender = messageToSend.Sender,
            TargetGroup = messageToSend.TargetGroup,
            Type = MessageType.DataPackage,
        };

        var connection = await getHubConnection();

        await connection.SendAsync("Dispatch", message);
    }

    public async Task<bool> StoreDataPackage(Message message)
    {
        if (message.Package?.FileDataid is null)
            throw new ArgumentException
            ($"Exception:{nameof(FileTransmissionManager)}.{nameof(StoreDataPackage)}:" +
                                        $"{nameof(message.Package.FileDataid)} is null.");

        Guid fileDataKey = message.Package.FileDataid;
        if (!_downloadedFileIdToPackages.ContainsKey(fileDataKey))
            _downloadedFileIdToPackages[fileDataKey] = new 
                (_messageBox.Messages
                    .First(x=>x.Type == MessageType.Metadata && x.Metadata.DataFileId == fileDataKey)
                    .Metadata.ChunksCount);

        if (_downloadedFileIdToPackages[fileDataKey].Any(x => x.Index == message.Package.Index))
            return false;

        _downloadedFileIdToPackages[fileDataKey].Add(await BuildClientPackageAsync(message));
        
        if (_downloadedFileIdToPackages[fileDataKey].Count == _messageBox.Messages
                .First(x=>x.Type == MessageType.Metadata && x.Metadata.DataFileId == fileDataKey)
                .Metadata.ChunksCount)
        {
            await _messageBox.AddMessageAsync(new ()
                {
                    Packages = _downloadedFileIdToPackages[fileDataKey],
                    Sender = message.Sender
                },
                isEncrypted: false);
            _downloadedFileIdToPackages.TryRemove(fileDataKey, out _);
            return true;
        }

        return false;
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

    public void HandlePackageRegisteredByHub(Guid fileId, int packageIndex)
    {
        if (!PackageCount.TryGetValue(fileId, out var _))
        {
            var metadata = _messageBox.Messages
                .Where(x => x.Type == MessageType.Metadata)
                .FirstOrDefault(x => x.Metadata?.DataFileId == fileId)
                ?.Metadata;

            if (metadata is not null)
                PackageCount.TryAdd(fileId, metadata.ChunksCount);
        }

        if (DownloadedPackages.ContainsKey(fileId))
            DownloadedPackages.TryUpdate(fileId, DownloadedPackages[fileId] + 1, DownloadedPackages[fileId]);
        else
            DownloadedPackages.TryAdd(fileId, 1);
        
        _callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnChunkLoaded");

        if (PackageCount[fileId] == DownloadedPackages[fileId])
        {
            _callbackExecutor.ExecuteSubscriptionsByName(fileId, "MessageRegisteredByHub");
            _uploadedFileIdToPackages.TryRemove(fileId, out _);
        }
    }

    private async Task<ClientPackage> BuildClientPackageAsync(Message message)
    {
        if (message.Package is null)
            throw new ArgumentException
            ($"Exception:{nameof(FileTransmissionManager)}.{nameof(BuildClientPackageAsync)}:" +
             $"{nameof(message.Package)} is null.");
        
        return new ()
        {
            B64Data = message.Package.B64Data,
            PlainB64Data = await GetDecryptedPackageBase64Async(message),
            Index = message.Package.Index,
            FileDataid = message.Package.FileDataid,
            IV = message.Package.IV,
        };
    }

    private async Task<string> GetDecryptedPackageBase64Async(Message message)
    {
        if (message.Package is null)
            throw new ArgumentException(
                $"Exception:{nameof(FileTransmissionManager)}.{nameof(GetDecryptedPackageBase64Async)}:" +
                $"{nameof(Message.Package)} is null.");
        
        if (string.IsNullOrWhiteSpace(message.Package.IV))
            throw new ArgumentException(
                $"Exception:{nameof(FileTransmissionManager)}.{nameof(GetDecryptedPackageBase64Async)}:" +
                $"{nameof(Message.Package.IV)} is empty string or null.");
        
        var aesKey = InMemoryKeyStorage.AESKeyStorage
            .FirstOrDefault(x => x.Key == message.Sender)
            .Value?.Value?.ToString();
        
        if (string.IsNullOrWhiteSpace(aesKey))
            throw new ArgumentException(
                $"Exception:{nameof(FileTransmissionManager)}.{nameof(GetDecryptedPackageBase64Async)}:" +
                $"AES key is empty string or null.");
        
        await _jsRuntime.InvokeVoidAsync("ImportIV", message.Package.IV);
        
        var decrypted = await _jsRuntime
            .InvokeAsync<string>("AESDecryptText", message.Package.B64Data, aesKey);

        return decrypted;
    }
}