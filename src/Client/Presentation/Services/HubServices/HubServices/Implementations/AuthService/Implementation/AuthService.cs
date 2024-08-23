using System.Collections.Concurrent;
using Client.Application.Gateway;
using Client.Infrastructure.Gateway;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.LocalStorageService;
using EthachatShared.Constants;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.Authentication.Models.UserAuthentication;
using EthachatShared.Models.Message;
using MessagePack;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.AuthService.Implementation;

public class AuthService : IAuthService
{
    private NavigationManager NavigationManager { get; set; }
    private readonly ICallbackExecutor _callbackExecutor;
    private readonly ILocalStorageService _localStorageService;
    private readonly ConcurrentQueue<Func<bool, Task>> _refreshTokenCallbackQueue = new();
    public ConcurrentQueue<Func<AuthResult, Task>> IsTokenValidCallbackQueue { get; set; } = new();
    private readonly IAuthenticationHandler _authenticationManager;
    private IGateway? _gateway;

    private async Task<IGateway> ConfigureGateway()
    {
        var gateway = new SignalRGateway();
        await gateway.ConfigureAsync(NavigationManager.ToAbsoluteUri(HubAddress.Auth));
        return gateway;
    }

    public AuthService
    (NavigationManager navigationManager,
        ICallbackExecutor callbackExecutor,
        ILocalStorageService localStorageService,
        IAuthenticationHandler authenticationManager)
    {
        NavigationManager = navigationManager;
        _callbackExecutor = callbackExecutor;
        _localStorageService = localStorageService;
        _authenticationManager = authenticationManager;
        _ = GetHubConnectionAsync();
    }

    public async Task<IGateway> GetHubConnectionAsync()
    {
        return await InitializeGatewayAsync();
    }

    private async Task<IGateway> InitializeGatewayAsync()
    {
        _gateway ??= await ConfigureGateway();

        await _gateway.AddEventCallbackAsync<AuthResult>("OnRefreshCredentials", async result =>
        {
            if (result.Result is not AuthResultType.Success)
                NavigationManager.NavigateTo("signin");

            if (result.JwtPair is not null)
                await _authenticationManager.UpdateCredentials(result.JwtPair);

            _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRefreshCredentials");
        });

        await _gateway.AddEventCallbackAsync<AuthResult>("OnValidateCredentials", result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnValidateCredentials");
                return Task.CompletedTask;
            });

        await _gateway.AddEventCallbackAsync<AuthResult>("OnLoggingIn",
            result => 
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnLogIn");
                return Task.CompletedTask;
            });

        await _gateway.AddEventCallbackAsync<List<AccessRefreshEventLog>>("OnRefreshTokenHistoryResponse", result =>
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRefreshTokenHistoryResponse");
                return Task.CompletedTask;
            });

        await _gateway.AddEventCallbackAsync<AuthResult, Guid>("OnCredentialIdRefresh", async (result, _) =>
        {
            var currentCounter =
                uint.Parse(await _localStorageService.ReadPropertyAsync("credentialIdCounter") ?? "0");
            if (result.Result == AuthResultType.Success)
            {
                await _localStorageService.WritePropertyAsync("credentialIdCounter",
                    (currentCounter + 1).ToString());
            }

            _callbackExecutor.ExecuteCallbackQueue(result.Result == AuthResultType.Success,
                _refreshTokenCallbackQueue);
        });

        await _gateway.AddEventCallbackAsync<AuthResult>("OnRegister",
            result => 
            {
                _callbackExecutor.ExecuteSubscriptionsByName(result, "OnRegister");
                return Task.CompletedTask;
            });

        return _gateway;
    }

    public async Task ValidateAccessTokenAsync(Func<AuthResult, Task> isTokenAccessValidCallback)
    {
        var authenticationIsReadyToUse = await _authenticationManager.IsSetToUseAsync();
        if (!authenticationIsReadyToUse)
        {
            await isTokenAccessValidCallback(new AuthResult() { Result = AuthResultType.Fail });
        }
        else
        {
            //Server will trigger callback execution when server responds us by calling
            //client 'OnAuthenticationCredentialsValidated' method with boolean value
            IsTokenValidCallbackQueue.Enqueue(isTokenAccessValidCallback);

            //Informing server that we're waiting for it's decision on access token
            await _authenticationManager.TriggerCredentialsValidation(await GetHubConnectionAsync());
        }
    }

    public async Task Register(UserAuthentication newUserDto)
    {
        _gateway ??= await ConfigureGateway();
        await _gateway.TransferAsync(new ClientToServerData
        {
            EventName = "Register",
            Data = MessagePackSerializer.Serialize(newUserDto),
            Type = typeof(UserAuthentication)
        });
    }

    public async Task LogIn(UserAuthentication userAuthentication)
    {
        _gateway ??= await ConfigureGateway();
        await _gateway.TransferAsync(new ClientToServerData
        {
            EventName = "LogIn",
            Data = MessagePackSerializer.Serialize(userAuthentication),
            Type = typeof(UserAuthentication)
        });
    }

    public async Task GetRefreshTokenHistory()
    {
        _gateway ??= await ConfigureGateway();
        var accessToken = await _authenticationManager.GetAccessCredential();
        await _gateway.TransferAsync(new ClientToServerData
        {
            EventName = "GetTokenRefreshHistory",
            Data = MessagePackSerializer.Serialize(accessToken),
            Type = typeof(string)
        });
    }
}