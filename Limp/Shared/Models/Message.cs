namespace Limp.Shared.Models
{
    public class Message
    {
        public string SenderConnectionId { get; set; }
        public string SenderUsername { get; set; }
        public string Payload { get; set; }
        public DateTime DateReceived { get; set; }
        public DateTime DateSent { get; set; }
    }
}
