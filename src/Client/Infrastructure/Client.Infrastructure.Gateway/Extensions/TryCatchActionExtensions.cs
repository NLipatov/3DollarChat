namespace Client.Infrastructure.Gateway.Extensions;

internal static class TryCatchActionExtensions
{
    internal static async Task SafeInvokeAsync<T1, T2>(this Func<T1, T2, Task> action, T1 t1Data, T2 t2Data)
    {
        try
        {
            await action(t1Data, t2Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    internal static async Task SafeInvokeAsync<T1>(this Func<T1, Task> action, T1 t1Data)
    {
        try
        {
            await action(t1Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    internal static async Task SafeInvokeAsync(this Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}