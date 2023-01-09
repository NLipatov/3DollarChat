namespace Limp.Shared.Models
{
    public class Message
    {
        public string TargetGroup { get; set; }
        public string SenderConnectionId { get; set; }
        public string CompanionConnectionId { get; set; }
        public string Sender { get; set; }
        public string Topic { get; set; }
        public string Payload { get; set; }
        public DateTime DateReceived { get; set; }
        public DateTime DateSent { get; set; }
    }
}
