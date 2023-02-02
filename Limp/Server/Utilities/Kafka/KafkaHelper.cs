using ClientServerCommon.Models;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace Limp.Server.Utilities.Kafka
{
    public class KafkaHelper : IMessageBrokerService, IHostedService
    {
        private Thread _thread;
        private IConfiguration _configuration;
        private const string TOPIC = "messages";
        public KafkaHelper(IConfiguration configuration)
        {
            var MessageBrokerSection = configuration.GetSection("MessageBrokerSettings");

            _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            new Dictionary<string, string>
            {
                { "bootstrap.servers", MessageBrokerSection.GetValue<string>("bootstrap.servers") },
                { "group.id",  MessageBrokerSection.GetValue<string>("group.id")},
                { "auto.offset.reset", MessageBrokerSection.GetValue<string>("auto.offset.reset") }
            })
            .Build(); ;
        }
        public async Task ProduceAsync(Message message)
        {
            using (var producer = new ProducerBuilder<string, string>(
                _configuration.AsEnumerable()).Build())
            {
                var result = await producer.ProduceAsync(TOPIC,
                    new Message<string, string> { Key = message.TargetGroup, Value = JsonSerializer.Serialize(message) });

                producer.Flush(TimeSpan.FromSeconds(10));
            }
        }

        public async Task<Thread> GetConsumerThread()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("info: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Starting Apache Kafka consumer thread.\n");

            HubConnection hubConnection = new HubConnectionBuilder()
            .WithUrl(_configuration.GetSection("MessageBrokerSettings").GetValue<string>("messageDispatcherHubAddress")!)
            .Build();

            await Task.Delay(10000);
            await hubConnection.StartAsync();

            using (var consumer = new ConsumerBuilder<string, string>(
                _configuration.AsEnumerable()).Build())
            {
                consumer.Subscribe(TOPIC);

                while (true)
                {
                    Console.WriteLine("waiting for any messages from Apache Kafka...");
                    var cr = consumer.Consume();
                    if (cr.Message.Value != null)
                    {
                        Message? message = JsonSerializer.Deserialize<Message>(cr.Message.Value);
                        if (message != null)
                        {
                            await hubConnection.SendAsync("Dispatch", message);
                        }
                    }
                }
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                _thread = await GetConsumerThread();
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _thread.Suspend();
            return Task.CompletedTask;
        }
    }
}
