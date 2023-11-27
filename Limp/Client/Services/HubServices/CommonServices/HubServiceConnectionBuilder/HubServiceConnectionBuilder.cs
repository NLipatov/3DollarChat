using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;

public static class HubServiceConnectionBuilder
{
    public static HubConnection Build(Uri hubAddress, bool useStatefulReconnect = true)
    {
        return new HubConnectionBuilder()
            .WithUrl(hubAddress, options =>
            {
                options.UseStatefulReconnect = useStatefulReconnect;
            })
            .AddMessagePackProtocol()
            .Build();
    }
}