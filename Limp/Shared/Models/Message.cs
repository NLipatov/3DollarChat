﻿using System.Text.Json;
using ICloneable = Limp.Shared.Models.Contracts.ICloneable<Limp.Shared.Models.Message>;

namespace Limp.Shared.Models
{
    public class Message : ICloneable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TargetGroup { get; set; }
        public string SenderConnectionId { get; set; }
        public string CompanionConnectionId { get; set; }
        public string Sender { get; set; }
        public string Topic { get; set; }
        public string Payload { get; set; }
        public bool IsReceived { get; set; }
        public DateTime DateReceived { get; set; }
        public DateTime DateSent { get; set; }

        public Message Clone()
        {
            return JsonSerializer
                .Deserialize<Message>
                (JsonSerializer.Serialize(this))!;
        }
    }
}
