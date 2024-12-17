using Ethachat.Client.Services.Authentication.Stages;

namespace Ethachat.Client.Services.Authentication.Boundaries;

public interface IAuthenticationManagerBoundary : IDisposable
{
    Task InitializeAsync();
    AuthenticationState AuthenticationState { get; }
}