using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.DriveService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Extensions;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;
using Ethachat.Client.Services.InboxService;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Strategies.ReceiveStrategies;

public class OnReceivedDriveStoredFileMessage(IMessageBox messageBox, IJSRuntime jsRuntime, IDriveService driveService) : IStrategyHandler<DriveStoredFileMessage>
{
    public async Task HandleAsync(DriveStoredFileMessage message)
    {
        var data = await driveService.DownloadAsync(message);
        messageBox.AddMessage(await message.ToClientMessage(data, jsRuntime));
    }
}