using System.Collections.Concurrent;

namespace Client.Infrastructure.Cryptography;

public class CryptoTaskQueue
{
    private readonly ConcurrentQueue<Func<Task>> _taskQueue = new();
    private TaskCompletionSource<bool> _queueSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CryptoTaskQueue()
    {
        Task.Run(async () => await ProcessQueueAsync());
    }

    public void EnqueueTask(Func<Task> task)
    {
        _taskQueue.Enqueue(task);
        _queueSignal.TrySetResult(true);
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            await _queueSignal.Task;

            _queueSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _semaphore.WaitAsync();
            try
            {
                while (_taskQueue.TryDequeue(out var task))
                {
                    await task();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}