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
            Type = MessageType.DataPackage
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
            _downloadedFileIdToPackages[fileDataKey] = new (message.Package.Total);

        if (_downloadedFileIdToPackages[fileDataKey].Any(x => x.Index == message.Package.Index))
            return false;

        _downloadedFileIdToPackages[fileDataKey].Add(await BuildClientPackageAsync(message));
        
        if (_downloadedFileIdToPackages[fileDataKey].Count == message.Package.Total)
        {
            await _messageBox.AddMessageAsync(new ()
                {
                    Packages = _downloadedFileIdToPackages[fileDataKey],
                    Sender = message.Sender,
                },
                isEncrypted: false);
            _downloadedFileIdToPackages.TryRemove(fileDataKey, out _);
            return true;
        }

        return false;
    }

    public void HandlePackageRegisteredByHub(Guid fileId, int packageIndex)
    {
        if (!_uploadedFilePackagesRemainTobeUploaded.TryGetValue(fileId, out int _))
            _uploadedFilePackagesRemainTobeUploaded.TryAdd(fileId,
                _messageBox.Messages.FirstOrDefault(x => x.Id == fileId)?.Packages.Count ?? 0);

        _uploadedFilePackagesRemainTobeUploaded.TryGetValue(fileId, out int previous);
        var current = previous - 1;

        _uploadedFilePackagesRemainTobeUploaded.TryUpdate(fileId, current, previous);
        
        _callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnChunkLoaded");

        if (current == 1)
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
            Total = message.Package.Total,
            IV = message.Package.IV,
            ContentType = message.Package.ContentType,
            FileName = message.Package.FileName
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