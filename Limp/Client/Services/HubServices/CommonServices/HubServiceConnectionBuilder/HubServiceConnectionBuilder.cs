using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;

public static class HubServiceConnectionBuilder
{
    public static HubConnection Build(Uri hubAddress)
    {
        return new HubConnectionBuilder()
            .WithUrl(hubAddress)
            .AddMessagePackProtocol()
            .Build();
    }
}