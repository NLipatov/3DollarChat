using System.Collections.Concurrent;
using System.Reflection;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.MessageTransmitionGateway;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.LongTermMessageStorage;
using Ethachat.Server.Hubs.MessageDispatcher.Handlers.ReliableMessageSender.ConcreteSenders.SenderImplementations.
    EncryptedData;
using Ethachat.Server.Hubs.UsersConnectedManaging.ConnectedUserStorage;
using EthachatShared.Models.Message;
using Moq;

namespace Ethachat.Server.Tests;

public class ClientToClientDataReliableSenderTests
{
    private readonly Mock<IMessageGateway<ClientToClientData>> _gatewayMock = new();
    private readonly Mock<ILongTermStorageService<ClientToClientData>> _longTermStorageServiceMock = new();

    [Fact]
    public async Task EnqueueAsync_ShouldAddItemToUnsentItems()
    {
        var reliableSender = new ClientToClientDataReliableSender(
            _gatewayMock.Object,
            _longTermStorageServiceMock.Object
        );

        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser1", ["testUser1"]);
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser2", ["testUser2"]);

        // Arrange
        var data = new ClientToClientData
            { Id = Guid.NewGuid(), Target = "testUser1", Sender = "testUser2", DataType = typeof(string) };

        // Act
        await reliableSender.EnqueueAsync(data);

        // Assert
        Assert.Single(GetUnsentItems(reliableSender));
    }

    [Fact]
    public async Task OnAck_ShouldRemoveItemFromQueueAndUnsentItems()
    {
        var reliableSender = new ClientToClientDataReliableSender(
            _gatewayMock.Object,
            _longTermStorageServiceMock.Object
        );

        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser1", ["testUser1"]);
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser2", ["testUser2"]);

        // Arrange
        var data = new ClientToClientData
            { Id = Guid.NewGuid(), Target = "testUser1", Sender = "testUser2", DataType = typeof(string) };
        await reliableSender.EnqueueAsync(data);

        // Act
        reliableSender.OnAck(data);

        await Task.Delay(1000);

        // Assert
        Assert.Empty(GetUnsentItems(reliableSender));
        Assert.Empty(GetMessageQueue(reliableSender));
    }

    [Fact]
    public async Task SendAsync_ShouldRequeueIfNotAcked()
    {
        var reliableSender = new ClientToClientDataReliableSender(
            _gatewayMock.Object,
            _longTermStorageServiceMock.Object
        );

        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser1", ["testUser1"]);
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser2", ["testUser2"]);

        // Arrange
        var data = new ClientToClientData
            { Id = Guid.NewGuid(), Target = "testUser1", Sender = "testUser2", DataType = typeof(string) };
        await reliableSender.EnqueueAsync(data);

        // Mock TransferAsync to do nothing
        _gatewayMock.Setup(x => x.TransferAsync(data)).Returns(Task.CompletedTask);

        // Act
        await InvokeSendAsyncMethod(reliableSender, new UnsentItem<ClientToClientData>
        {
            Item = data,
            Backoff = TimeSpan.FromSeconds(1)
        });

        // Assert
        Assert.Single(GetUnsentItems(reliableSender)); // It should still be in the unsentItems
    }

    [Fact]
    public async Task SendAsync_ShouldNotRequeueIfAcked()
    {
        var reliableSender = new ClientToClientDataReliableSender(
            _gatewayMock.Object,
            _longTermStorageServiceMock.Object
        );

        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser1", ["testUser1"]);
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser2", ["testUser2"]);

        // Arrange
        var data = new ClientToClientData
            { Id = Guid.NewGuid(), Target = "testUser1", Sender = "testUser2", DataType = typeof(string) };
        await reliableSender.EnqueueAsync(data);
        reliableSender.OnAck(data); // Simulate acknowledgement

        // Mock TransferAsync to do nothing
        _gatewayMock.Setup(x => x.TransferAsync(data)).Returns(Task.CompletedTask);

        // Act
        await InvokeSendAsyncMethod(reliableSender, new UnsentItem<ClientToClientData>
        {
            Item = data,
            Backoff = TimeSpan.FromSeconds(1),
        });

        // Assert
        Assert.Empty(GetUnsentItems(reliableSender)); // It should not be in unsentItems
        Assert.Empty(GetMessageQueue(reliableSender)); // It should not be re-enqueued
    }

    [Fact]
    public async Task ProcessQueueAsync_ShouldProcessItemsCorrectly()
    {
        var reliableSender = new ClientToClientDataReliableSender(
            _gatewayMock.Object,
            _longTermStorageServiceMock.Object
        );

        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser1", ["testUser1"]);
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser2", ["testUser2"]);

        // Arrange
        var data1 = new ClientToClientData
            { Id = Guid.NewGuid(), Target = "testUser1", Sender = "testUser2", DataType = typeof(string) };
        var data2 = new ClientToClientData
            { Id = Guid.NewGuid(), Target = "testUser1", Sender = "testUser2", DataType = typeof(string) };

        await reliableSender.EnqueueAsync(data1);
        await reliableSender.EnqueueAsync(data2);

        // Mock TransferAsync to do nothing
        _gatewayMock.Setup(x => x.TransferAsync(It.IsAny<ClientToClientData>())).Returns(Task.CompletedTask);

        // Act
        await Task.Delay(100); // Give some time for the queue to process

        // Assert
        Assert.Equal(2,
            GetUnsentItems(reliableSender).Count); // Both items should still be in unsentItems if not acked
    }

    [Fact]
    public async Task ShouldProcess10000MessagesWithRandomAckTimes()
    {
        var reliableSender = new ClientToClientDataReliableSender(
            _gatewayMock.Object,
            _longTermStorageServiceMock.Object
        );

        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser1", ["testUser1"]);
        InMemoryHubConnectionStorage.MessageDispatcherHubConnections.TryAdd("testUser2", ["testUser2"]);

        // Arrange
        int totalMessages = 10000;
        var random = new Random();
        var messages = Enumerable.Range(0, totalMessages)
            .Select(_ => new ClientToClientData
            {
                Id = Guid.NewGuid(),
                Target = "testUser1",
                Sender = "testUser2",
                DataType = typeof(string)
            })
            .ToArray();

        foreach (var message in messages)
        {
            await reliableSender.EnqueueAsync(message);
        }

        // Mock TransferAsync to simulate sending the message
        _gatewayMock.Setup(x => x.TransferAsync(It.IsAny<ClientToClientData>()))
            .Returns(Task.CompletedTask);

        // Act
        var ackTasks = messages.Select(async message =>
        {
            // Simulate random delay before ACK (between 10ms to 100ms)
            await Task.Delay(random.Next(10, 100));
            reliableSender.OnAck(message);
        });

        await Task.WhenAll(ackTasks);

        // Assert

        // Use reflection to access private fields

        var unsentItems = GetUnsentItems(reliableSender);
        var messageQueue = GetMessageQueue(reliableSender);

        // Assert
        Assert.Empty(unsentItems);
        Assert.Empty(messageQueue);
    }

    private ConcurrentDictionary<Guid, UnsentItem<ClientToClientData>> GetUnsentItems(
        ClientToClientDataReliableSender reliableSender)
    {
        var unsentItemsField = typeof(ClientToClientDataReliableSender)
            .GetField("_unsentItems", BindingFlags.NonPublic | BindingFlags.Instance);

        var unsentItems =
            (ConcurrentDictionary<Guid, UnsentItem<ClientToClientData>>?)unsentItemsField?.GetValue(reliableSender)
            ?? throw new ApplicationException("Failed to get collection via reflection.");

        return unsentItems;
    }

    private ConcurrentQueue<Guid> GetMessageQueue(ClientToClientDataReliableSender reliableSender)
    {
        var messageQueueField = typeof(ClientToClientDataReliableSender)
            .GetField("_messageQueue", BindingFlags.NonPublic | BindingFlags.Instance);

        var messageQueue = (ConcurrentQueue<Guid>)messageQueueField!.GetValue(reliableSender)!;

        return messageQueue;
    }

    private async Task InvokeSendAsyncMethod(ClientToClientDataReliableSender reliableSender,
        UnsentItem<ClientToClientData> unsentItem)
    {
        var sendAsyncMethod = typeof(ClientToClientDataReliableSender)
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        var task = (Task?)sendAsyncMethod?.Invoke(reliableSender, [unsentItem])
                   ?? throw new ApplicationException("Failed to call sendAsync method.");
        await task;
    }
}