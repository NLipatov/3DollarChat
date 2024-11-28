using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Transfer.Domain.Entities.Events;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.Authentication.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Models;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Factory;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Strategies.ReceiveStrategies;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Strategies.SendStrategies;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.Types;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling;

public class TransferProcessorResolver : ITransferProcessorResolver
{
    private readonly Dictionary<Type, IMessageProcessor> _typeToProcessor = [];

    public TransferProcessorResolver(IMessageService messageService, ICallbackExecutor callbackExecutor,
        IMessageBox messageBox, IKeyStorage keyStorage, IAuthenticationHandler authenticationHandler,
        IBinaryReceivingManager binaryReceivingManager, ICryptographyService cryptographyService, IJSRuntime jsRuntime, 
        NavigationManager navigationManager)
    {
        RegisterProcessor<TextMessage>([
            new()
            {
                TransferDirection = TransferDirection.Incoming,
                Handler = new OnReceivedTextMessage(messageBox, authenticationHandler, messageService)
            }
        ]);

        RegisterProcessor<EventMessage>([
            new()
            {
                TransferDirection = TransferDirection.Incoming,
                Handler = new OnReceivedEventMessage(messageBox, callbackExecutor, messageService, keyStorage)
            },
            new()
            {
                TransferDirection = TransferDirection.Outcoming,
                Handler = new OnSentEventMessage(messageService)
            }
        ]);

        RegisterProcessor<Package>(
        [
            new()
            {
                TransferDirection = TransferDirection.Incoming,
                Handler = new OnReceivedDataPackage(callbackExecutor, binaryReceivingManager, messageService)
            }
        ]);

        RegisterProcessor<KeyMessage>([
            new()
            {
                TransferDirection = TransferDirection.Incoming,
                Handler = new OnReceivedKeyMessage(keyStorage, messageService, cryptographyService, callbackExecutor)
            }
        ]);

        RegisterProcessor<HlsPlaylistMessage>([
            new()
            {
                TransferDirection = TransferDirection.Incoming,
                Handler = new OnReceivedHlsPlaylist(messageBox)
            }
        ]);
        
        RegisterProcessor<DriveStoredFileMessage>([
            new ()
            {
                TransferDirection = TransferDirection.Incoming,
                Handler = new OnReceivedDriveStoredFileMessage(messageBox, jsRuntime, navigationManager)
            }]);
    }

    private void RegisterProcessor<T>(HandlerArguments<T>[] arguments)
    {
        var handlerFactory = new TransferHandlerFactory<T>();

        foreach (var argument in arguments)
            handlerFactory.RegisterHandler(GetEventName<T>(argument.TransferDirection), argument.Handler);

        MessageProcessor<T> processor = new(handlerFactory);

        _typeToProcessor.Add(typeof(T), processor);
    }

    public string GetEventName<T>(TransferDirection direction)
    {
        string?[] arguments = [direction.ToString(), typeof(T).ToString()];
        return string.Join('_', arguments.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public MessageProcessor<T> GetProcessor<T>()
    {
        _typeToProcessor.TryGetValue(typeof(T), out var processor);
        return (processor ?? throw new ArgumentException("Could not found a processor")) as MessageProcessor<T> ??
               throw new ArgumentException("Could not convert processor to a required type");
    }
}