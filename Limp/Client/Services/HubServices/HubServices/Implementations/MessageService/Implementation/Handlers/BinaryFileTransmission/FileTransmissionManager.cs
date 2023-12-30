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
    private ConcurrentDictionary<Guid, List<ClientPackage>> _uploadedFileIdToPackages = new();
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

    public async Task SendPackage(ClientMessage message, Func<Task<HubConnection>> getHubConnection)
    {
        try
        {
            await AddDataToMessageBox(message);
            foreach (var file in message.ClientFiles)
            {
                _uploadedFileIdToPackages.TryAdd(file.Id, file.ClientPackages);

                for (int i = 0; i < file.Packages.Count; i++)
                {
                    await SendPackageMessage(file.Id, file.Packages[i], message, getHubConnection);
                }
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException($"{nameof(FileTransmissionManager)}.{nameof(SendPackage)}: {e.Message}.");
        }
    }
    
    private async Task SendPackageMessage(Guid fileId, Package package, ClientMessage messageToSend, Func<Task<HubConnection>> getHubConnection)
    {
        _uploadedFileIdToPackages.TryGetValue(fileId, out var packages);

        if (packages is null || packages.All(x => x.Index != package.Index))
        {
            return;
        }

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

    public async Task AddDataToMessageBox(ClientMessage message)
    {
        var dataMessages = message.ClientFiles.Select(x => new ClientMessage()
        {
            Id = x.Packages.First().FileDataid,
            Sender = message.Sender,
            TargetGroup = message.TargetGroup,
            DateSent = DateTime.UtcNow,
            Type = MessageType.DataPackage,
            Packages = x.ClientPackages,
        }).ToList();

        await _messageBox.AddMessagesAsync(dataMessages.ToArray(), false);
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
        _callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnChunkLoaded");

        _uploadedFileIdToPackages.TryGetValue(fileId, out var sendedFilePackages);

        if (sendedFilePackages is null)
            return;

        var updatedPackages = sendedFilePackages.Where(x => x.Index != packageIndex).ToList();
        _uploadedFileIdToPackages.TryUpdate(fileId, updatedPackages, sendedFilePackages);

        if (!updatedPackages.Any())
        {
            _callbackExecutor.ExecuteSubscriptionsByName(fileId, "MessageRegisteredByHub");
            _uploadedFileIdToPackages.TryRemove(fileId, out updatedPackages);
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