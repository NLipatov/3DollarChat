using System.Diagnostics.CodeAnalysis;
using Client.Application.Runtime;
using Microsoft.JSInterop;

namespace Client.Infrastructure.Runtime.PlatformRuntime;

public class JsPlatformRuntime(IJSRuntime jsRuntime) : IPlatformRuntime
{
    public async ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        TValue>(string identifier, object?[]? args) =>
        await jsRuntime.InvokeAsync<TValue>(identifier, args);

    public async ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        await jsRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);

    public async ValueTask InvokeVoidAsync(string identifier, object?[]? args) =>
        await jsRuntime.InvokeVoidAsync(identifier, args);

    public async ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken,
        object?[]? args) =>
        await jsRuntime.InvokeVoidAsync(identifier, cancellationToken, args);
}