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
    private List<string> _linkedFiles = new();
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

    private async Task<FFMPEG> GetFf()
    {
        return _ff ??= await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false, () => Console.WriteLine("FINISH!"));
    }

    public async Task<HlsPlaylist> ConvertMp4ToM3U8(byte[] mp4)
    {
        var convertationDone = false;
        _ff = await FfmpegInitializationManager.InitializeAsync(_jsRuntime, withLog: false, () =>
        {
            convertationDone = true;
        });
        
        VideoId = Guid.NewGuid().ToString();
        //Loading original mp4 to a in-memory emscripten filesystem
        var filename = $"{VideoId}.mp4";
        (await GetFf()).WriteFile(filename, mp4);
        
        //Generating a KEY and IV
        var key  = await KeyFileGenerator.GetKey();
        var iv = await KeyFileGenerator.GetIv();

        //Create a key file with given key and load it to emscripten filesystem
        var keyFileUri = await CreateKeyFileURL(key);
        //Creates a infofile with given IV and keyFileUri
        var keyInfoFilename = await GetInfoFilenameAsync(keyFileUri, iv);

        await Convert(keyInfoFilename);
        
        while (!convertationDone)
        {
            Console.WriteLine("waiting for convertation to finish");
            await Task.Delay(20);
        }

        await GetM3U8Url();
        
        var M3U8Content = string.Empty;
        using (var streamReader = new StreamReader(new MemoryStream(await (await GetFf()).ReadFile($"{VideoId}.m3u8"))))
        {
            M3U8Content = await streamReader.ReadToEndAsync();
        }
        var generatedTsFiles = await ReadTsFileFromMemory(M3U8Content);

        var playlist = new HlsPlaylist
        {
            HexIv = iv,
            HexKey = key,
            Name = $"{VideoId}.m3u8",
            TsFiles = generatedTsFiles,
            M3U8Content = M3U8Content
        };

        using (var httpClient = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        {
            foreach (var tsFile in generatedTsFiles)
            {
                var fileContent = new ByteArrayContent(tsFile.Content);
                formData.Add(fileContent, "payload", tsFile.Name);
            }

            var test = _navigationManager.ToAbsoluteUri("hlsapi/store");
            var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/store");
            var response = await httpClient.PostAsync(endpointAddress, formData);
        }
        
        return playlist;
    }

    private async Task<string> CreateKeyFileURL(string hexKey)
    {
        //Generate a key file
        var keyFile = KeyFileGenerator.GenerateKeyFile(hexKey);
        //Create a blob with key file
        var keyFileUri = await _jsRuntime.InvokeAsync<string>("createBlobUrl", keyFile, "application/octet-stream");

        //store key file in emscripten filesystem
        var keyfileName = $"{VideoId}enc.key";
        (await GetFf()).WriteFile(keyfileName, keyFile);

        //For later dispose
        _linkedFiles.Add(keyfileName);
        _blobUrls.Add(keyFileUri);

        return keyFileUri;
    }

    private async Task<string> GetInfoFilenameAsync(string keyFileUri, string iv)
    {
        var keyInfoFileContent = $"{keyFileUri}\n{VideoId}enc.key\n{iv}";
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

    private async Task<List<TsFile>> ReadTsFileFromMemory(string m3U8Content)
    {        
        var tsFiles = new List<TsFile>();
        
        var lines = m3U8Content.Split("\n");
        foreach (var line in lines)
        {
            if (line.StartsWith(VideoId))
            {
                var tsBytes = await (await GetFf()).ReadFile(line);
                tsFiles.Add(new TsFile()
                {
                    Name = line,
                    Content = tsBytes
                });
            }
        }

        return tsFiles;
    }

    private async Task GetM3U8Url()
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