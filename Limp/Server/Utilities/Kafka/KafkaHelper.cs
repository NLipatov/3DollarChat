using Confluent.Kafka;
using Limp.Client.Pages;
using Limp.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using System.Xml.Linq;

namespace Limp.Server.Utilities.Kafka
{
    public class KafkaHelper : IMessageBrokerService, IHostedService
    {
        private Thread _thread;
        private IConfiguration _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            new Dictionary<string, string>
            {
                { "bootstrap.servers", "79.137.202.134:9092" },
                { "group.id",  "kafka-dotnet-getting-started"},
                { "auto.offset.reset", "earliest" }
            })
            .Build();
        private const string TOPIC = "messages";
        public KafkaHelper()
        {

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
            .WithUrl("https://localhost:7273/messageDispatcherHub")
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
                    if(cr.Message.Value != null)
                    {
                        Message? message = JsonSerializer.Deserialize<Message>(cr.Message.Value);
                        if (message != null)
                        {
                            Console.WriteLine(message.SenderUsername);
                            Console.WriteLine(message.TargetGroup);
                            Console.WriteLine(message.Payload);


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
                _thread = await this.GetConsumerThread();
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _thread.Suspend();
            return Task.CompletedTask;
        }
    }
}
