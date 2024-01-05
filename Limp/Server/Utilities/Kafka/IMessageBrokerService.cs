using EthachatShared.Models.Message;

namespace Ethachat.Server.Utilities.Kafka
{
    public interface IMessageBrokerService
    {
        Task ProduceAsync(Message message);
        Task<Thread> GetConsumerThread();
    }
}