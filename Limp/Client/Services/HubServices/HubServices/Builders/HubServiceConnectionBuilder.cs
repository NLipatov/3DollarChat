using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.HubServices.Builders;

public static class HubServiceConnectionBuilder
{
    public static HubConnection Build(Uri hubAddress, bool useStatefulReconnect = true) 
        => new HubConnectionBuilder()
        .WithUrl(hubAddress, options => { options.UseStatefulReconnect = useStatefulReconnect; })
        .AddMessagePackProtocol()
        .Build();
}