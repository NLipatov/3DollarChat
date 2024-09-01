using MessagePack;

namespace Ethachat.Client.Extensions;

public static class MessagePackExtensions
{
    public static async Task<T> DeserializeAsync<T>(this byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        return await MessagePackSerializer.DeserializeAsync<T>(memoryStream);
    }
    
    public static async Task<byte[]> SerializeAsync<T>(this T entity)
    {
        using var memoryStream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, entity);
        return memoryStream.ToArray();
    }
}