using Limp.Shared.Models;

namespace Limp.Server.Utilities.Kafka
{
    public interface IMessageBrokerService
    {
        Task ProduceAsync(Message message);
        Task<Thread> GetConsumerThread();
    }
}