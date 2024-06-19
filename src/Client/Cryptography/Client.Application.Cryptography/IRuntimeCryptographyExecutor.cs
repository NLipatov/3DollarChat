using System.Diagnostics.CodeAnalysis;

namespace Client.Application.Cryptography;

public interface IRuntimeCryptographyExecutor
{
    ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        TValue>(
        string identifier,
        object?[]? args);

    ValueTask<TValue> InvokeAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicFields |
                                    DynamicallyAccessedMemberTypes.PublicProperties)]
        TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args);

    ValueTask InvokeVoidAsync(string identifier, object?[]? args);

    ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken, object?[]? args);
}