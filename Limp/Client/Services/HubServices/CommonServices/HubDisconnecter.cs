using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.CommonServices
{
    public static class HubDisconnecter
    {
        public static async Task DisconnectAsync(HubConnection? hubConnection)
        {
            if (hubConnection == null)
                return;

            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
        }
    }
}
