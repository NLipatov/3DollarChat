using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.VideoStreamingService.Converters.FFmpeg.KeyFileGenerator;

public class KeyGenerator
{
    private string Id { get; init; }
    private readonly IJSRuntime _jsRuntime;
    private static ConcurrentDictionary<string, Action<string>> Callbacks { get; set; } = new();
    private string _hexKeyString = string.Empty;

    public KeyGenerator(IJSRuntime jsRuntime, string videoId)
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

    public async Task<string> GenerateKeyFileContentAsync()
    {
        var key = await GenerateAes128HexKey();

        return "{\n    \"method\": \"AES-128\",\n    \"key\": \"" + key + "\"\n}";
    }

    public async Task<string> GenerateAes128HexKey()
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