using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Extensions;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedDriveStoredFileMessage(IMessageBox messageBox, IJSRuntime jsRuntime, NavigationManager navigationManager) : IStrategyHandler<DriveStoredFileMessage>
{
    public async Task HandleAsync(DriveStoredFileMessage message)
    {
        var data = await LoadMessageDataAsync(message.Id);
        messageBox.AddMessage(await message.ToClientMessage(data, jsRuntime));
    }
    
    private async Task<byte[]> LoadMessageDataAsync(Guid messageId)
    {
        using var httpClient = new HttpClient();
        var getDataUrl = string.Join("", navigationManager.BaseUri, "driveapi/get?id=", messageId);
        var request = await httpClient.GetAsync(getDataUrl);
        return await request.Content.ReadAsByteArrayAsync();
    }
}