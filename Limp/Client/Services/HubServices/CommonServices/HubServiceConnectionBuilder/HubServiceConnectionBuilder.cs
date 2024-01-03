using Microsoft.AspNetCore.SignalR.Client;

namespace Ethachat.Client.Services.HubServices.CommonServices.HubServiceConnectionBuilder;

public static class HubServiceConnectionBuilder
{
    public static HubConnection Build(Uri hubAddress, bool useStatefulReconnect = true)
    {
        Console.WriteLine($"{nameof(HubServiceConnectionBuilder)}.{nameof(Build)}: Building hub connection at {hubAddress}");
        HubConnection hubConnection;
        try
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(hubAddress, options =>
                {
                    options.UseStatefulReconnect = useStatefulReconnect;
                })
                .AddMessagePackProtocol()
                .Build();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return hubConnection;
    }
}