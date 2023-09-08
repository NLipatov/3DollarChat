using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.Extensions
{
    public static class HubConnectionExtensions
    {
        public static async Task DisconnectAsync(this HubConnection? hubConnection)
        {
            if (hubConnection == null)
                return;

            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
        }
    }
}
