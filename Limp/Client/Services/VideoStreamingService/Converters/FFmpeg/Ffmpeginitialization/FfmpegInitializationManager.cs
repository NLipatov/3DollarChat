using FFmpegBlazor;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Ffmpeginitialization;

public class FfmpegInitializationManager
{
    public async Task<FFMPEG> InitializeAsync(IJSRuntime _jsRuntime, bool withLog = false,
        Action OnRunToCompletionCallback = null)
    {
        try
        {
            FFMPEG ff;
            await FFmpegFactory.Init(_jsRuntime);
            ff = FFmpegFactory.CreateFFmpeg(new FFmpegConfig() { Log = withLog });
            await ff.Load();
            if (!ff.IsLoaded)
            {
                throw new ApplicationException($"Could not load {nameof(ff)}");
            }

            if (OnRunToCompletionCallback is not null)
            {
                await RegisterProgressCallback(ff, _jsRuntime, OnRunToCompletionCallback);
            }

            return ff;
        }
        catch (Exception e)
        {
            throw new ApplicationException($"Could not initialize {nameof(FFMPEG)}");
        }
    }

    private async Task RegisterProgressCallback(FFMPEG ff, IJSRuntime _jsRuntime, Action OnRunToCompletionCallback)
    {
        FFmpegFactory.Progress += async e =>
        {
            if (e.Ratio >= 1) //ratio >= 1 means that convert job is done
            {
                OnRunToCompletionCallback();
            }
        };
    }
}