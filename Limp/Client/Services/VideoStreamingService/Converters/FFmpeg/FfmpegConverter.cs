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
    private List<string> _emscryptenFilesystemFiles = new();
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
        (await GetFf()).WriteFile(originalMp4Filename, mp4);
        
        //It will create a single .m3u8 file and some .ts files from original mp4
        await Convert(keyInfoFilename);
        
        //keyinfo and key files were used by ffmpeg, we can delete them now from emscripten memory
        (await GetFf()).UnlinkFile($"{VideoId}enc.keyinfo");
        (await GetFf()).UnlinkFile($"{VideoId}enc.key");

        //wait till ffmpeg finishes it's job
        while (!convertationDone)
        {
            await Task.Delay(20);
        }
        
        //Since convertation is done, we can remove original mp4 file from emcripten filesystem
        (await GetFf()).UnlinkFile(originalMp4Filename);

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
        var keyFileUri = await _jsRuntime.InvokeAsync<string>("createBlobUrl", keyFile, "application/octet-stream");

        //store key file in emscripten filesystem
        var keyfileName = $"{VideoId}enc.key";
        (await GetFf()).WriteFile(keyfileName, keyFile);

        //For later dispose
        _emscryptenFilesystemFiles.Add(keyfileName);
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
        _emscryptenFilesystemFiles.Add(keyInfoFilename);

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
                        //Read media segment file generated by ffmpeg and remove it from emscripten filesystem
                        var tsBytes = await (await GetFf()).ReadFile(line);
                        (await GetFf()).UnlinkFile(line);
                        
                        using (var formData = new MultipartFormDataContent())
                        {
                            var fileContent = new ByteArrayContent(tsBytes);
                            formData.Add(fileContent, "payload", line);
                        
                            var endpointAddress = string.Join("", _navigationManager.BaseUri, "hlsapi/store");
                            var request = await httpClient.PostAsync(endpointAddress, formData);
                            if (!request.IsSuccessStatusCode)
                                throw new ApplicationException("Failed to store ts file on server");
                        }
                    
                        sb.AppendLine(_navigationManager.BaseUri + "hlsapi/get?filename=" + line);
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

    public async ValueTask DisposeAsync()
    {
        foreach (var blobUrl in _blobUrls)
        {
            await _jsRuntime.InvokeVoidAsync("revokeBlobUrl", blobUrl);
        }

        foreach (var linkedFile in _emscryptenFilesystemFiles)
        {
            (await GetFf()).UnlinkFile(linkedFile);
        }
    }
}