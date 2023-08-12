namespace Limp.Client.Services.HubServices.CommonServices.SubscriptionService.Types
{
    public record class Callback
    {
        public Type? CallbackType { get; set; }
        public object? Delegate { get; set; }
    }
}