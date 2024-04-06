using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using FFmpegBlazor;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Ffmpeginitialization;

public class FfmpegInitializationManager
{
    private ICallbackExecutor? _callbackExecutor { get; set; }
    public async Task<FFMPEG> InitializeAsync(IJSRuntime _jsRuntime, bool withLog = false,
        Action OnRunToCompletionCallback = null, ICallbackExecutor? callbackExecutor = null)
    {
        try
        {
            _callbackExecutor = callbackExecutor;
            FFMPEG ff;
            await FFmpegFactory.Init(_jsRuntime);
            ff = FFmpegFactory.CreateFFmpeg(new FFmpegConfig() { Log = withLog });
            
            _callbackExecutor?.ExecuteSubscriptionsByName($"Loading ff","OnStatusUpdate");
            try
            {
                await ff.Load();
            }
            catch (Exception e)
            {
                _callbackExecutor?.ExecuteSubscriptionsByName($"Could not load ff: {e.Message}","OnStatusUpdate");
                Console.WriteLine(e);
                throw;
            }
            if (!ff.IsLoaded)
            {
                throw new ApplicationException($"Could not load {nameof(ff)}");
            }
            _callbackExecutor?.ExecuteSubscriptionsByName($"ff is loaded: {ff.IsLoaded}","OnStatusUpdate");

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
            _callbackExecutor?.ExecuteSubscriptionsByName($"Convertation progress: {Math.Round(e.Ratio * 100, 0)}%","OnStatusUpdate");
            if (e.Ratio >= 1) //ratio >= 1 means that convert job is done
            {
                OnRunToCompletionCallback();
            }
        };
    }
}