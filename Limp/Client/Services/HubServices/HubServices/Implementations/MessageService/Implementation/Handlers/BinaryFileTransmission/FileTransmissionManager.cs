using System.Collections.Concurrent;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Cryptography.KeyStorage;
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
    private ConcurrentDictionary<Guid, List<ClientPackage>> _downloadedFileIdToPackages = new();
    private ConcurrentDictionary<Guid, List<int>> _uploadedFileIdToPackages = new();
    private ConcurrentDictionary<Guid, int> _uploadedFilePackagesRemainTobeUploaded = new();
    private ConcurrentDictionary<Guid, int> UploadedPackages = new();
    private ConcurrentDictionary<Guid, int> DownloadedPackages = new();
    private ConcurrentDictionary<Guid, int> PackageCount = new();
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

    public async Task SendMetadata(Message message, Func<Task<HubConnection>> getHubConnection)
    {
        if (message.Metadata is null)
            throw new ArgumentException($"Exception:{nameof(FileTransmissionManager)}.{nameof(SendMetadata)}: Invalid metadata.");
        
        var connection = await getHubConnection();

        await connection.SendAsync("Dispatch", message);
    }

    public async Task SendFile(ClientMessage message, Func<Task<HubConnection>> getHubConnection)
    {
        var fileDataId = Guid.NewGuid();
        var chunkableBinary = await _packageMultiplexerService.SplitAsync(message.BrowserFile);
        int totalChunks = chunkableBinary.Count;
        var metadataMessage = GenerateMetadataMessage(fileDataId, message, totalChunks);
        
        await _messageBox.AddMessageAsync(metadataMessage, false);
        
        await SendViaHubConnectionAsync(new Message()
        {
            Type = metadataMessage.Type,
            Sender = metadataMessage.Sender,
            TargetGroup = metadataMessage.TargetGroup,
            Metadata = metadataMessage.Metadata,
        }, getHubConnection);

        await AddBinaryAsBlobToMessageBox(metadataMessage.Metadata, message.BrowserFile, message.Sender, message.TargetGroup);
        
        int chunksCounter = 0;
        await foreach (var chunk in chunkableBinary.GenerateChunksAsync())
        {
            var cryptogram = await _cryptographyService.EncryptAsync<AESHandler>(new()
            {
                Cyphertext = chunk
            }, message.TargetGroup);

            var package = new ClientPackage()
            {
                Index = chunksCounter,
                B64Data = cryptogram.Cyphertext,
                IV = cryptogram.Iv,
                FileDataid = fileDataId,
                PlainB64Data = chunk
            };
            
            var packageMessage = new Message
            {
                Id = fileDataId,
                Package = package,
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
            var blobUrl = await _jsRuntime.InvokeAsync<string>("createBlobUrl", memoryStream.ToArray(), metadata.ContentType);
                
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

        await SendViaHubConnectionAsync(message, getHubConnection);
    }

    private async Task SendViaHubConnectionAsync(Message message, Func<Task<HubConnection>> getHubConnection)
    {
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
                    Sender = message.Sender,
                    Type = MessageType.DataPackage
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