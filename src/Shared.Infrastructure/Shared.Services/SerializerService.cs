using MessagePack;

namespace SharedServices;

public class SerializerService : ISerializerService
{
    public async Task<T> DeserializeAsync<T>(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        return await MessagePackSerializer.DeserializeAsync<T>(memoryStream);
    }

    public async Task<byte[]> SerializeAsync<T>(T entity)
    {
        using var memoryStream = new MemoryStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, entity);
        return memoryStream.ToArray();
    }
}