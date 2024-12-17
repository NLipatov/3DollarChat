namespace SharedServices;

public interface ISerializerService
{
    Task<T> DeserializeAsync<T>(byte[] bytes);
    Task<byte[]> SerializeAsync<T>(T entity);
}