namespace Client.Infrastructure.Cryptography.Tests;

public class CryptoTaskQueueTests
{
    [Fact]
    public async Task EnqueueTask_SingleTask_TaskIsExecuted()
    {
        // Arrange
        var queue = new CryptoTaskQueue();
        var source = new TaskCompletionSource<bool>();

        // Act
        queue.EnqueueTask(() =>
        {
            source.SetResult(true);
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(await source.Task);
    }


    [Fact]
    public async Task EnqueueTask_100Tasks_TasksAreExecuted()
    {
        // Arrange
        var queue = new CryptoTaskQueue();
        var sources = new List<TaskCompletionSource<bool>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var source = new TaskCompletionSource<bool>();
            sources.Add(source);
            queue.EnqueueTask(() =>
            {
                source.SetResult(true);
                return Task.CompletedTask;
            });
        }

        // Assert
        await Task.WhenAll(sources.Select(source => source.Task));
        Assert.All(sources, source => Assert.True(source.Task.IsCompletedSuccessfully));
    }
}