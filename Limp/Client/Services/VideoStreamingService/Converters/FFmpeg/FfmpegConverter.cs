using System.Text;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Ffmpeginitialization;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.HlsEncryption;
using Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.Models;
using FFmpegBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg;

public class FfmpegConverter : IAsyncDisposable
{
    private FfmpegInitializationManager FfmpegInitializationManager { get; } = new();
    private List<string> _emscryptenFiles = new();
    private List<string> _blobUrls = new();
    private string VideoId { get; set; }
    private FFMPEG? _ff;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private HlsEncryptionManager KeyFileGenerator { get; }

    public FfmpegConverter(IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        VideoId = Guid.NewGuid().ToString();
        KeyFileGenerator = new HlsEncryptionManager(jsRuntime, VideoId);
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
    }

    //Gets FFmpeg instance (existing one or creates a new one)
    private async Task<FFMPEG> GetFf()
    {
        return _ff ??= await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false, null);
    }

    public async Task<HlsPlaylist> Mp4ToM3U8(byte[] mp4)
    {
        var convertationDone = false;
        _ff = await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false,
            () => { convertationDone = true; });

        VideoId = Guid.NewGuid().ToString();

        //Generating a KEY and IV
        var key = await KeyFileGenerator.GetKey();
        var iv = await KeyFileGenerator.GetIv();

        //Create a key file with given key and load it to emscripten filesystem
        var keyFileUri = await CreateKeyFileURL(key);
        //Creates a infofile with given IV and keyFileUri
        var keyInfoFilename = await GetInfoFilenameAsync(keyFileUri, iv);

        //Loading original mp4 to a in-memory emscripten filesystem
        var originalMp4Filename = $"{VideoId}.mp4";
        await WriteToEmscripten(originalMp4Filename, mp4);
        
        //It will create a single .m3u8 file and some .ts files from original mp4
        await Convert(keyInfoFilename);

        //wait till ffmpeg finishes it's job
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

        return playlist;
    }

    private async Task<string> CreateKeyFileURL(string hexKey)
    {
        //Generate a key file
        var keyFile = KeyFileGenerator.GenerateKeyFile(hexKey);
        //Create a blob with key file
        var keyFileUri = await ToBlobUrl(keyFile, "application/octet-stream");

        //store key file in emscripten filesystem
        var keyfileName = $"{VideoId}enc.key";
        await WriteToEmscripten(keyfileName, keyFile);

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
            "-i", 
            $"{VideoId}.mp4", //original file
            "-codec", "copy", //copies codec instead of re-encoding — no quality loss and faster
            "-f", "segment", //original file will be splitted into media segments(.ts files)
            "-copyts", //time marks will be copied out of original file with no changes
            "-hls_time", "5", //segment duration in seconds
            "-segment_list", $"{VideoId}.m3u8", //specifies output playlist file, that will contain all the segments
            $"{VideoId}_%d.ts" //template for naming segment files
        };

        //specifies encryption file information
        if (!string.IsNullOrWhiteSpace(keyInfoFilename))
        {
            ffmpegArgs.Add("-hls_key_info_file");
            ffmpegArgs.Add(keyInfoFilename);
        }

        await (await GetFf()).Run(
            ffmpegArgs.ToArray());
    }

    private async Task<string> GenerateM3U8()
    {
        var playlistName = $"{VideoId}.m3u8";
        
        //Read generated by ffmpeg m3u8 playlist and remove it from emscripten filesystem
        var playlistContent =
            await (await GetFf()).ReadFile(playlistName);
        (await GetFf()).UnlinkFile(playlistName);
        
        StringBuilder sb = new();
        using (var streamReader = new StreamReader(new MemoryStream(playlistContent)))
        {
            var content = await streamReader.ReadToEndAsync();
            var lines = content.Split("\n");

            using (var httpClient = new HttpClient())
            {
                foreach (var line in lines)
                {
                    if (line.StartsWith(VideoId))
                    {
                        sb.AppendLine(_navigationManager.BaseUri + "hlsapi/get?filename=" + line);
                        
                        //Read media segment file generated by ffmpeg and remove it from emscripten filesystem
                        var tsBytes = await (await GetFf()).ReadFile(line);
                        (await GetFf()).UnlinkFile(line);

                        using var formData = new MultipartFormDataContent();
                        var fileContent = new ByteArrayContent(tsBytes);
                        formData.Add(fileContent, "payload", line);
                        
                        var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/store");
                        var request = await httpClient.PostAsync(endpointAddress, formData);
                        if (!request.IsSuccessStatusCode)
                            throw new ApplicationException("Failed to store ts file on server");

                        Console.WriteLine("Stored on server: " + line);
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }

            var modifiedPlaylist = sb.ToString();
            return modifiedPlaylist;
        }
    }

    private async Task WriteToEmscripten(string filename, byte[] bytes)
    {
        (await GetFf()).WriteFile(filename, bytes);
        _emscryptenFiles.Add(filename);
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

        foreach (var linkedFile in _emscryptenFiles)
        {
            (await GetFf()).UnlinkFile(linkedFile);
        }

        _ff = null;
    }
}