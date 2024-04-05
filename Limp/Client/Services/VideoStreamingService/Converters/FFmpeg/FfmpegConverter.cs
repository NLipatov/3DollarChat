using System.Text;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Ffmpeginitialization;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.HlsEncryption;
using Ethachat.Client.Services.VideoStreamingService.Extensions;
using EthachatShared.Models.Message;
using FFmpegBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;

public class FfmpegConverter : IAsyncDisposable
{
    private FfmpegInitializationManager FfmpegInitializationManager { get; init; }
    private List<string> _emscriptenFiles = new();
    private List<string> _blobUrls = new();
    private string VideoId { get; set; }
    private FFMPEG? _ff;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly ICallbackExecutor _callbackExecutor;
    private HlsEncryptionManager KeyFileGenerator { get; }

    public FfmpegConverter(IJSRuntime jsRuntime, NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor)
    {
        VideoId = Guid.NewGuid().ToString();
        KeyFileGenerator = new HlsEncryptionManager(jsRuntime, VideoId);
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        _callbackExecutor = callbackExecutor;
        FfmpegInitializationManager = new();
    }

    //Gets FFmpeg instance (existing one or creates a new one)
    private async Task<FFMPEG> GetFf()
    {
        return _ff ??= await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false, null, _callbackExecutor);
    }

    public async Task<HlsPlaylist> Mp4ToM3U8(byte[] videoBytes)
    {
        _callbackExecutor.ExecuteSubscriptionsByName($"Converting mp4 to m3u8","OnStatusUpdate");
        var convertationDone = false;
        _ff = await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false,
            () => { convertationDone = true; }, _callbackExecutor);

        VideoId = Guid.NewGuid().ToString();

        //Generating a KEY and IV
        var key = await KeyFileGenerator.GetKey();
        var iv = await KeyFileGenerator.GetIv();

        //Create a key file with given key and load it to emscripten filesystem
        var keyFileUri = await CreateKeyFileUrl(key);
        //Creates a infofile with given IV and keyFileUri
        var keyInfoFilename = await GetInfoFilenameAsync(keyFileUri, iv);

        //Loading original mp4 to a in-memory emscripten filesystem
        var originalMp4Filename = $"{VideoId}.mp4";
        await WriteToEmscripten(originalMp4Filename, videoBytes);

        //It will create a single .m3u8 file and some .ts files from original mp4
        await ConvertMp4ToM3U8(keyInfoFilename);

        //wait till ffmpeg finishes it's job
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (!convertationDone)
        {
            await Task.Delay(20);
        }

        var playlist = new HlsPlaylist
        {
            HexIv = iv,
            HexKey = key,
            Name = $"{VideoId}.m3u8",
            M3U8Content = await GenerateM3U8()
        };
        
        _callbackExecutor.ExecuteSubscriptionsByName($"Converted mp4 to m3u8","OnStatusUpdate");

        return playlist;
    }

    private async Task<string> CreateKeyFileUrl(string hexKey)
    {
        //Generate a key file
        var keyFileBytes = KeyFileGenerator.GenerateKeyFile(hexKey);
        //Create a blob with key file
        var keyFileUri = await ToBlobUrl(keyFileBytes, "application/octet-stream");

        //store key file in emscripten filesystem
        var keyfileName = $"{VideoId}enc.key";
        await WriteToEmscripten(keyfileName, keyFileBytes);

        return keyFileUri;
    }

    private async Task<string> GetInfoFilenameAsync(string keyFileUri, string iv)
    {
        var keyInfoFileContent = $"{keyFileUri}\n{VideoId}enc.key\n{iv}";
        var keyInfoFileBytes = Encoding.UTF8.GetBytes(keyInfoFileContent);
        var keyInfoFilename = $"{VideoId}enc.keyinfo";
        await WriteToEmscripten(keyInfoFilename, keyInfoFileBytes);

        return keyInfoFilename;
    }

    private async Task ConvertMp4ToM3U8(string keyInfoFilename)
    {
        if (string.IsNullOrWhiteSpace(keyInfoFilename))
            throw new ArgumentException($"Invalid {nameof(keyInfoFilename)} value");

        var keyInfoContent = await (await GetFf()).ReadFile(keyInfoFilename);

        if (!keyInfoContent.Any())
            throw new ArgumentException($"Invalid {nameof(keyInfoContent)} value.");

        var ffmpegArgs = new List<string>
        {
            "-i",
            $"{VideoId}.mp4",
            "-c:v", "copy",
            "-c:a", "copy",
            "-hls_key_info_file", keyInfoFilename,
            "-hls_playlist_type", "vod",
            "-hls_time", "10",
            $"{VideoId}.m3u8"
        };

        await (await GetFf()).Run(
            ffmpegArgs.ToArray());
    }

    private async Task<string> GenerateM3U8()
    {
        _callbackExecutor.ExecuteSubscriptionsByName($"Generating m3u8 playlist","OnStatusUpdate");
        var playlistName = $"{VideoId}.m3u8";

        //Read generated by ffmpeg m3u8 playlist and remove it from emscripten filesystem
        var playlistContent =
            await (await GetFf()).ReadFile(playlistName);
        (await GetFf()).UnlinkFile(playlistName);

        StringBuilder sb = new();
        using var streamReader = new StreamReader(new MemoryStream(playlistContent));
        var content = await streamReader.ReadToEndAsync();
        var lines = content.Split("\n");
        var tsCounter = 0;
        var tsTotal = lines.Count(x => x.StartsWith(VideoId));
        _callbackExecutor.ExecuteSubscriptionsByName(tsTotal, "OnTotalSegmentsCountResolved");

        foreach (var line in lines)
        {
            if (line.StartsWith(VideoId))
            {
                var tsFilename = $"{VideoId}_{tsCounter}.ts";
                tsCounter++;
                sb.AppendLine(_navigationManager.BaseUri + "hlsapi/get?filename=" + tsFilename);
                await Task.Run(async () =>
                {
                    await SendFormData(line, tsFilename);
                }).ConfigureAwait(false);
                _callbackExecutor.ExecuteSubscriptionsByName(true, "OnHLSMediaSegmentUploaded");
            }
            else
            {
                sb.AppendLine(line);
            }
        }
        
        _callbackExecutor.ExecuteSubscriptionsByName((false, ""), "OnHLSStreamingPreparationInProgress");

        var modifiedPlaylist = sb.ToString();
        _callbackExecutor.ExecuteSubscriptionsByName($"m3u8 playlist generation completed","OnStatusUpdate");
        return modifiedPlaylist;
    }

    private async Task SendFormData(string line, string segmentName)
    {
        //Read media segment file generated by ffmpeg and remove it from emscripten filesystem
        var tsBytes = await (await GetFf()).ReadFile(line);
        (await GetFf()).UnlinkFile(line);

        using var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(tsBytes);
        formData.Add(fileContent, "payload", segmentName);

        var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/store");
        using var client = new HttpClient();
        await client.PostAsync(endpointAddress, formData);
    }

    private async Task WriteToEmscripten(string filename, byte[] bytes)
    {
        (await GetFf()).WriteFile(filename, bytes);
        _emscriptenFiles.Add(filename);
    }

    private async Task<string> ToBlobUrl(byte[] bytes, string mimeType)
    {
        var url = await _jsRuntime.InvokeAsync<string>("createBlobUrl", bytes, mimeType);
        _blobUrls.Add(url);
        return url;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var blobUrl in _blobUrls)
        {
            await _jsRuntime.InvokeVoidAsync("revokeBlobUrl", blobUrl);
        }

        foreach (var linkedFile in _emscriptenFiles)
        {
            (await GetFf()).UnlinkFile(linkedFile);
        }

        _ff = null;
    }

    public async Task<byte[]> ConvertToMp4(byte[] videoBytes, ExtentionType type)
    {
        var convertedFilename = $"{VideoId}.mp4";
        var unconvertedFilename = $"{VideoId}.{type.ToString().ToLower()}";
        await WriteToEmscripten(unconvertedFilename, videoBytes);
        _callbackExecutor.ExecuteSubscriptionsByName($"File was written to emscripten filesystem","OnStatusUpdate");

        var ffmpegArgs = new List<string>
        {
            "-i",
            unconvertedFilename,
            "-c:v", "libx264",
            "-preset", "medium",
            "-crf", "23",
            "-c:a", "aac",
            "-b:a", "128k",
            convertedFilename
        };
        _callbackExecutor.ExecuteSubscriptionsByName($"Starting a convertation...","OnStatusUpdate");

        await (await GetFf()).Run(
            ffmpegArgs.ToArray());

        _callbackExecutor.ExecuteSubscriptionsByName($"Reading the output file from emscripten filesystem","OnStatusUpdate");
        var bytes = await (await GetFf()).ReadFile(convertedFilename);
        _callbackExecutor.ExecuteSubscriptionsByName($"Output file was read from emscripten filesystem","OnStatusUpdate");
        (await GetFf()).UnlinkFile(convertedFilename);
        (await GetFf()).UnlinkFile(unconvertedFilename);
        return bytes;
    }
}