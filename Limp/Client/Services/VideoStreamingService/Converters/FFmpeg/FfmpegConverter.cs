using System.Text;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Ffmpeginitialization;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.HlsEncryption;
using EthachatShared.Models.Message.VideoStreaming;
using FFmpegBlazor;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;

public class FfmpegConverter : IAsyncDisposable
{
    private FfmpegInitializationManager FfmpegInitializationManager { get; } = new();
    private HlsVideoStreamingDetails HlsVideoStreamingDetails { get; } = new();
    private List<string> _linkedFiles = new();
    private List<string> _blobUrls = new();
    private string VideoId { get; set; }
    private FFMPEG? _ff;
    private readonly IJSRuntime _jsRuntime;
    private HlsEncryptionManager KeyFileGenerator { get; }

    public FfmpegConverter(IJSRuntime jsRuntime)
    {
        VideoId = Guid.NewGuid().ToString();
        KeyFileGenerator = new HlsEncryptionManager(jsRuntime, VideoId);
        _jsRuntime = jsRuntime;
    }

    private async Task<FFMPEG> GetFf()
    {
        return _ff ??= await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false, () => Console.WriteLine("FINISH!"));
    }

    public async Task<HlsVideoStreamingDetails> ConvertMp4ToM3U8(byte[] mp4)
    {
        var convertationDone = false;
        _ff = await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false, () =>
        {
            convertationDone = true;
        });
        
        HlsVideoStreamingDetails.HexKey = await KeyFileGenerator.GetKey();
        HlsVideoStreamingDetails.HexIv = await _jsRuntime.InvokeAsync<string>("GenerateIVForHLS");
        VideoId = Guid.NewGuid().ToString();

        //write to in-memory emscripten files
        var filename = $"{VideoId}.mp4";
        (await GetFf()).WriteFile(filename, mp4);
        _linkedFiles.Add(filename);

        var keyFileUri = await CreateKeyFile();
        var keyInfoFilename = await GetInfoFilenameAsync(keyFileUri);

        await Convert(keyInfoFilename);
        
        while (!convertationDone)
        {
            Console.WriteLine("waiting for convertation to finish");
            await Task.Delay(20);
        }

        HlsVideoStreamingDetails.PlaylistUrl = await GetM3U8Url();

        return HlsVideoStreamingDetails;
    }

    private async Task<string> CreateKeyFile()
    {
        var keyFile = KeyFileGenerator.GetKeyFile(HlsVideoStreamingDetails.HexKey);
        var keyFileUri = await _jsRuntime.InvokeAsync<string>("createBlobUrl", keyFile, "application/octet-stream");

        var filename = $"{VideoId}enc.key";
        (await GetFf()).WriteFile(filename, keyFile);

        //For later dispose
        _linkedFiles.Add(filename);
        _blobUrls.Add(keyFileUri);

        return keyFileUri;
    }

    private async Task<string> GetInfoFilenameAsync(string keyFileUri)
    {
        var keyInfoFileContent = $"{keyFileUri}\n{VideoId}enc.key\n{HlsVideoStreamingDetails.HexIv}";
        var keyInfoFileBytes = Encoding.UTF8.GetBytes(keyInfoFileContent);
        var keyInfoFilename = $"{VideoId}enc.keyinfo";
        (await GetFf()).WriteFile(keyInfoFilename, keyInfoFileBytes);

        //For later dispose
        _linkedFiles.Add(keyInfoFilename);

        return keyInfoFilename;
    }

    private async Task Convert(string? keyInfoFilename = "")
    {
        if (!string.IsNullOrWhiteSpace(keyInfoFilename))
        {
            //check if key info file is readable
            var keyInfoContent = await (await GetFf()).ReadFile(keyInfoFilename);
            if (!keyInfoContent.Any())
            {
                throw new ArgumentException(
                    $"Invalid {nameof(keyInfoFilename)} argument: {keyInfoFilename} file could not be read.");
            }
        }

        var ffmpegArgs = new List<string>
        {
            "-i", $"{VideoId}.mp4",
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
        ffmpegArgs.Add($"{VideoId}.m3u8");

        await (await GetFf()).Run(
            ffmpegArgs.ToArray());
    }

    private async Task<string> GetM3U8Url()
    {
        var playlistName = $"{VideoId}.m3u8";
        var playlistContent =
            await (await GetFf()).ReadFile(playlistName);
        StringBuilder sb = new();

        using (var streamReader = new StreamReader(new MemoryStream(playlistContent)))
        {
            var content = await streamReader.ReadToEndAsync();
            var lines = content.Split("\n");
            foreach (var line in lines)
            {
                if (line.StartsWith(VideoId))
                {
                    var tsBytes = await (await GetFf()).ReadFile(line);
                    (await GetFf()).UnlinkFile(line);
                    var tsUrl = FFmpegFactory
                        .CreateURLFromBuffer(tsBytes, Guid.NewGuid().ToString(), "video/mp2t");
                    sb.AppendLine(tsUrl);
                    _blobUrls.Add(tsUrl);
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
            FFmpegFactory.CreateURLFromBuffer(playlistContent, $"{VideoId}.m3u8", "application/vnd.apple.mpegurl");
        _blobUrls.Add(playlistUrl);

        return playlistUrl;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var blobUrl in _blobUrls)
        {
            await _jsRuntime.InvokeVoidAsync("revokeBlobUrl", blobUrl);
        }

        foreach (var linkedFile in _linkedFiles)
        {
            (await GetFf()).UnlinkFile(linkedFile);
        }
    }
}