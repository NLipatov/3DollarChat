using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.Services.VideoStreamingService;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageInput.Logic.MessageSenders.BrowserFile;

public class BrowserFileSender(
    IAuthenticationHandler authenticationHandler,
    IHlsStreamingService hlsStreamingService,
    ICallbackExecutor callbackExecutor,
    NavigationManager navigationManager,
    IMessageBox messageBox,
    IMessageService messageService,
    IBinarySendingManager binarySendingManager) : IBrowserFileSender
{
    public async Task SendIBrowserFile(IBrowserFile browserFile, string target)
    {
        //stream it if it can be streamed
        if (await hlsStreamingService.CanFileBeStreamedAsync(browserFile.Name))
        {
            var playlist = await PostVideoToHlsApiAsync(browserFile);

            if (playlist is not null)
            {
                callbackExecutor.ExecuteSubscriptionsByName(false, "OnIsFileBeingEncrypted");

                var playlistMessage = new HlsPlaylistMessage
                {
                    Id = Guid.NewGuid(),
                    Playlist = playlist.M3U8Content,
                    Target = target,
                    Sender = await authenticationHandler.GetUsernameAsync(),
                };

                messageBox.AddMessage(playlistMessage);

                await messageService.TransferAsync(playlistMessage);
                return;
            }
        }

        callbackExecutor.ExecuteSubscriptionsByName(true, "OnIsFileBeingEncrypted");

        byte[] fileBytes;
        await using (var fileStream = browserFile.OpenReadStream(long.MaxValue))
        {
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }

        var package = new Package
        {
            Id = Guid.NewGuid(),
            Data = fileBytes,
            ContentType = browserFile.ContentType,
            Filename = browserFile.Name,
            Target = target,
            Sender = await authenticationHandler.GetUsernameAsync(),
        };

        await foreach (var dataPartMessage in binarySendingManager.GetChunksToSendAsync(package))
            _ = Task.Run(async () => await messageService.TransferAsync(dataPartMessage));

        callbackExecutor.ExecuteSubscriptionsByName(false, "OnIsFileBeingEncrypted");
    }


    private async Task<HlsPlaylist?> PostVideoToHlsApiAsync(IBrowserFile browserFile)
    {
        callbackExecutor.ExecuteSubscriptionsByName($"HLS: Uploading a {browserFile.Name}", "OnTitleChange");
        callbackExecutor.ExecuteSubscriptionsByName(true, "OnShouldRender");
        var formData = new MultipartFormDataContent();

        var progressStreamContent =
            new ProgressStreamContent(browserFile.OpenReadStream(long.MaxValue), browserFile.Size);
        progressStreamContent.ProgressChanged += (_, currBytes, totalBytes) =>
        {
            var ratio = currBytes / (double)totalBytes;
            var percentage = Math.Round(ratio * 100, 1);
            callbackExecutor.ExecuteSubscriptionsByName(percentage, "OnVideoUploadProgressChanged");
        };

        formData.Add(progressStreamContent, "payload", browserFile.Name);

        using var httpClient = new HttpClient();
        try
        {
            var hlsApiUrl = string.Join("", navigationManager.BaseUri, "hlsapi/convert");

            var request = await httpClient.PostAsync(hlsApiUrl, formData);

            if (!request.IsSuccessStatusCode)
                throw new ApplicationException("Could not post video to HLS API");

            var m3U8PlaylistContent = await request.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(m3U8PlaylistContent))
                return null;

            return new HlsPlaylist
            {
                Name = browserFile.Name,
                M3U8Content = m3U8PlaylistContent,
            };
        }
        catch (Exception e)
        {
            throw new ApplicationException("Could not convert video to HLS", e);
        }
        finally
        {
            callbackExecutor.ExecuteSubscriptionsByName(false, "OnShouldRender");
        }
    }
}