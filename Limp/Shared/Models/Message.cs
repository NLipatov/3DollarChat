namespace Limp.Shared.Models
{
    public class Message
    {
        public string TargetGroup { get; set; }
        public string SenderConnectionId { get; set; }
        public string CompanionConnectionId { get; set; }
        public string SenderUsername { get; set; }
        public string Payload { get; set; }
        public DateTime DateReceived { get; set; }
        public DateTime DateSent { get; set; }
    }
}
