namespace Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService.Types
{
    public class Subscription
    {
        public Guid ComponentId { get; set; }
        public string SubscriptionName { get; set; } = string.Empty;
        public Callback? Callback { get; set; }
    }
}
