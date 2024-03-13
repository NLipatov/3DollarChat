using System.Text;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.KeyFileGenerator;
using FFmpegBlazor;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;

public class FfmpegConverter
{
    private List<string> LinkedFiles = new();
    private List<string> blobUrls = new();
    private string videoId { get; set; }
    private FFMPEG ff { get; set; }
    private readonly IJSRuntime _jsRuntime;
    private KeyGenerator keyFileGenerator { get; init; }

    public FfmpegConverter(IJSRuntime jsRuntime, ICallbackExecutor callbackExecutor)
    {
        videoId = Guid.NewGuid().ToString();
        keyFileGenerator = new KeyGenerator(jsRuntime, videoId);
        _jsRuntime = jsRuntime;
    }

    private async Task<FFMPEG> InitializeFfAsync()
    {
        ff = FFmpegFactory.CreateFFmpeg(new FFmpegConfig() { Log = true });
        await ff.Load();
        if (!ff.IsLoaded)
        {
            throw new ApplicationException($"Could not load {nameof(ff)}");
        }

        return ff;
    }

    public async Task ConvertMp4ToM3U8(byte[] mp4)
    {
        videoId = Guid.NewGuid().ToString();
        await _jsRuntime.InvokeVoidAsync("GenerateAESKeyForHLS", videoId);

        await FFmpegFactory.Init(_jsRuntime);
        ff = await InitializeFfAsync();
        FFmpegFactory.Progress += async e =>
        {
            if (e.Ratio >= 1) //ratio >= 1 means that convert job is done
            {
                //get m3u8 url
                var m3U8Url = await GetM3U8Url();
                //start playback
                await _jsRuntime.InvokeVoidAsync("startStream", m3U8Url);
            }
        };

        //write to in-memory emscripten files
        var filename = $"{videoId}.mp4";
        ff.WriteFile(filename, mp4);
        LinkedFiles.Add(filename);

        var keyFileUri = await CreateKeyFile();
        var keyInfoFilename = await GetInfoFilenameAsync(keyFileUri);

        await Convert(mp4, keyInfoFilename);
    }

    private async Task<string> CreateKeyFile()
    {
        var keyFileContent = await keyFileGenerator.GenerateKeyFileContentAsync();
        var keyFile = Encoding.UTF8.GetBytes(keyFileContent);
        var keyFileUri = await _jsRuntime.InvokeAsync<string>("createBlobUrl", keyFile, "application/octet-stream");

        var filename = $"{videoId}enc.key";
        ff.WriteFile(filename, keyFile);

        //For later dispose
        LinkedFiles.Add(filename);
        blobUrls.Add(keyFileUri);

        return keyFileUri;
    }

    private async Task<string> GetInfoFilenameAsync(string keyFileUri)
    {
        var iv = await _jsRuntime.InvokeAsync<string>("GenerateIVForHLS");
        var keyInfoFileContent = $"{keyFileUri}\n{videoId}enc.key\n{iv}";
        var keyInfoFileBytes = Encoding.UTF8.GetBytes(keyInfoFileContent);
        var keyInfoFilename = $"{videoId}enc.keyinfo";
        ff.WriteFile(keyInfoFilename, keyInfoFileBytes);

        //For later dispose
        LinkedFiles.Add(keyInfoFilename);

        return keyInfoFilename;
    }

    private async Task Convert(byte[] bytes, string? keyInfoFilename = "")
    {
        if (!string.IsNullOrWhiteSpace(keyInfoFilename))
        {
            //check if key info file is readable
            var keyInfoContent = await ff.ReadFile(keyInfoFilename);
            if (!keyInfoContent.Any())
            {
                throw new ArgumentException(
                    $"Invalid {nameof(keyInfoFilename)} argument: {keyInfoFilename} file could not be read.");
            }
        }

        var ffmpegArgs = new List<string>
        {
            "-i", $"{videoId}.mp4",
            "-codec", "copy",
            "-hls_time", "10",
            "-hls_key_info_file", "enc.keyinfo"
        };

        //specifies encryption file information
        if (!string.IsNullOrWhiteSpace(keyInfoFilename))
        {
            ffmpegArgs.Add("-hls_key_info_file");
            ffmpegArgs.Add(keyInfoFilename);
        }

        //specifies output file name
        ffmpegArgs.Add($"{videoId}.m3u8");

        await ff.Run(
            ffmpegArgs.ToArray());
    }

    private async Task<string> GetM3U8Url()
    {
        var playlistName = $"{videoId}.m3u8";
        var playlistContent = await ff.ReadFile(playlistName);
        StringBuilder sb = new();

        using (var streamReader = new StreamReader(new MemoryStream(playlistContent)))
        {
            var content = await streamReader.ReadToEndAsync();
            var lines = content.Split("\n");
            foreach (var line in lines)
            {
                if (line.StartsWith(videoId))
                {
                    var tsBytes = await ff.ReadFile(line);
                    ff.UnlinkFile(line);
                    var tsUrl = FFmpegFactory
                        .CreateURLFromBuffer(tsBytes, Guid.NewGuid().ToString(), "video/mp2t");
                    sb.AppendLine(tsUrl);
                    blobUrls.Add(tsUrl);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            var modifiedPlaylist = sb.ToString();
            playlistContent = Encoding.UTF8.GetBytes(modifiedPlaylist);
        }

        var playlistUrl =
            FFmpegFactory.CreateURLFromBuffer(playlistContent, $"{videoId}.m3u8", "application/vnd.apple.mpegurl");
        blobUrls.Add(playlistUrl);
        return playlistUrl;
    }
}