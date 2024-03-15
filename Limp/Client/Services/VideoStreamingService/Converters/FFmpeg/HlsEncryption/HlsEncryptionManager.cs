using System.Collections.Concurrent;
using System.Text;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.HlsEncryption;

public class HlsEncryptionManager
{
    private string Id { get; init; }
    private readonly IJSRuntime _jsRuntime;
    private static ConcurrentDictionary<string, Action<string>> Callbacks { get; set; } = new();
    private string _hexKeyString = string.Empty;

    public HlsEncryptionManager(IJSRuntime jsRuntime, string videoId)
    {
        Id = videoId;
        _jsRuntime = jsRuntime;
    }

    [JSInvokable]
    public static async void OnHlsKeyReady(string key, string id)
    {
        if (Callbacks.TryGetValue(id, out var callback))
        {
            callback.Invoke(key);
        }
    }

    public byte[] GetKeyFile(string hexKey) =>
        Encoding.UTF8.GetBytes("{\n    \"method\": \"AES-128\",\n    \"key\": \"" + hexKey + "\"\n}");

    public async Task<string> GetIv() => await _jsRuntime.InvokeAsync<string>("GenerateIVForHLS");

    /// <summary>
    /// Get AES-CBC 128 key to encrypt media fragments
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public async Task<string> GetKey()
    {
        Callbacks.TryAdd(Id, ((key) => _hexKeyString = key));

        await _jsRuntime.InvokeAsync<string>("GenerateAESKeyForHLS", Id);

        var counter = 0;

        while (string.IsNullOrWhiteSpace(_hexKeyString) && counter < 20)
        {
            counter++;
            await Task.Delay(150);
        }

        return _hexKeyString ?? throw new ApplicationException("Could not generate AES key for HLS video encryption");
    }
}