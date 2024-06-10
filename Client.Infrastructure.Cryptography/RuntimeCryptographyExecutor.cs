using System.Diagnostics.CodeAnalysis;
using Client.Application.Cryptography;

namespace Client.Infrastructure.Cryptography;

public class RuntimeCryptographyExecutor(IPlatformRuntime runtime) : IRuntimeCryptographyExecutor
{
    public async ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        TValue>(
        string identifier,
        object?[]? args) =>
        await ExecuteWithExceptionHandling(() => runtime.InvokeAsync<TValue>(identifier, args));

    public async ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args) => await ExecuteWithExceptionHandling(() =>
        runtime.InvokeAsync<TValue>(identifier, cancellationToken, args));

    public async ValueTask InvokeVoidAsync(string identifier, object?[]? args) =>
        await ExecuteWithExceptionHandling(() => runtime.InvokeVoidAsync(identifier, args));

    public async ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        await ExecuteWithExceptionHandling(() => runtime.InvokeVoidAsync(identifier, cancellationToken, args));

    private async ValueTask<T> ExecuteWithExceptionHandling<T>(Func<ValueTask<T>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"An error occurred: {ex.Message}", ex);
        }
    }

    private async ValueTask ExecuteWithExceptionHandling(Func<ValueTask> func)
    {
        try
        {
            await func();
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"An error occurred: {ex.Message}", ex);
        }
    }
}