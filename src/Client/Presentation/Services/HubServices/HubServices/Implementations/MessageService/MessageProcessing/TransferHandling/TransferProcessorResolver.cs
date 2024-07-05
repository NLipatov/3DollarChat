using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Strategies.ReceiveStrategies;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Strategies.SendStrategies;
using Ethachat.Client.Services.InboxService;
using EthachatShared.Encryption;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.DataTransfer;
using EthachatShared.Models.Message.KeyTransmition;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling;

public class TransferProcessorResolver : ITransferProcessorResolver
{
    private Dictionary<Type, object> _typeToProcessor = [];
    private MessageProcessor<TextMessage> _textMessageProcessor;
    private MessageProcessor<EventMessage> _eventMessageProcessor;
    private MessageProcessor<Package> _packageProcessor;
    private MessageProcessor<AesOffer> _aesOfferProcessor;
    private MessageProcessor<KeyMessage> _keyMessageProcessor;
    private MessageProcessor<HlsPlaylistMessage> _hlsPlaylistMessageProcessor;

    public TransferProcessorResolver(IMessageService messageService, ICallbackExecutor callbackExecutor,
        IMessageBox messageBox, IKeyStorage keyStorage, IAuthenticationHandler authenticationHandler,
        IBinarySendingManager binarySendingManager, IBinaryReceivingManager
            binaryReceivingManager, ICryptographyService cryptographyService)
    {
        var textMessageReceivedHandlerFactory = new TransferHandlerFactory<TextMessage>();
        var eventMessageTransferReceivedHandlerFactory = new TransferHandlerFactory<EventMessage>();
        var packageTransferReceivedHandlerFactory = new TransferHandlerFactory<Package>();
        var aesOfferTransferReceivedHandlerFactory = new TransferHandlerFactory<AesOffer>();
        var keyMessageTransferReceivedHandlerFactory = new TransferHandlerFactory<KeyMessage>();
        var hlsPlaylistMessageTransferReceivedHandlerFactory = new TransferHandlerFactory<HlsPlaylistMessage>();

        packageTransferReceivedHandlerFactory.RegisterHandler(GetEventName<Package>(TransferDirection.Incoming),
            new DataPackageReceivedStrategy(callbackExecutor, binaryReceivingManager, messageService));
        packageTransferReceivedHandlerFactory.RegisterHandler(GetEventName<Package>(TransferDirection.Outcoming),
            new SendFileStrategy(messageService, binarySendingManager));

        hlsPlaylistMessageTransferReceivedHandlerFactory.RegisterHandler(
            GetEventName<HlsPlaylistMessage>(TransferDirection.Incoming),
            new HlsPlaylistReceivedStrategy(messageBox));

        textMessageReceivedHandlerFactory.RegisterHandler(GetEventName<TextMessage>(TransferDirection.Incoming),
            new TextMessageReceivedStrategy(messageBox, authenticationHandler, messageService));

        textMessageReceivedHandlerFactory.RegisterHandler(GetEventName<TextMessage>(TransferDirection.Outcoming),
            new SendTextStrategy(messageService, authenticationHandler, messageBox));

        aesOfferTransferReceivedHandlerFactory.RegisterHandler(GetEventName<AesOffer>(TransferDirection.Incoming),
            new AesOfferReceivedStrategy(keyStorage, messageService, callbackExecutor));
        
        eventMessageTransferReceivedHandlerFactory.RegisterHandler(GetEventName<EventMessage>(TransferDirection.Incoming),
            new EventMessageReceivedStrategy(messageBox, callbackExecutor, messageService, keyStorage));

        keyMessageTransferReceivedHandlerFactory.RegisterHandler(KeyType.RsaPublic.ToString(),
            new RsaPubKeyMessageRequestReceivedStrategy(keyStorage, cryptographyService, authenticationHandler,
                messageService));
        keyMessageTransferReceivedHandlerFactory.RegisterHandler(KeyType.Aes.ToString(),
            new AesKeyMessageReceivedStrategy(keyStorage, messageService, callbackExecutor));

        _textMessageProcessor = new(textMessageReceivedHandlerFactory);
        _eventMessageProcessor = new(eventMessageTransferReceivedHandlerFactory);
        _packageProcessor = new(packageTransferReceivedHandlerFactory);
        _aesOfferProcessor = new(aesOfferTransferReceivedHandlerFactory);
        _keyMessageProcessor = new(keyMessageTransferReceivedHandlerFactory);
        _hlsPlaylistMessageProcessor = new(hlsPlaylistMessageTransferReceivedHandlerFactory);
        _typeToProcessor.Add(typeof(TextMessage), _textMessageProcessor);
        _typeToProcessor.Add(typeof(EventMessage), _eventMessageProcessor);
        _typeToProcessor.Add(typeof(Package), _packageProcessor);
        _typeToProcessor.Add(typeof(AesOffer), _aesOfferProcessor);
        _typeToProcessor.Add(typeof(KeyMessage), _keyMessageProcessor);
        _typeToProcessor.Add(typeof(HlsPlaylistMessage), _hlsPlaylistMessageProcessor);
    }

    public string GetEventName<T>(TransferDirection direction)
    {
        string?[] arguments = [direction.ToString(), typeof(T).ToString()];
        return string.Join('_', arguments.Where(x=>!string.IsNullOrWhiteSpace(x)));
    }

    public MessageProcessor<T> GetProcessor<T>()
    {
        _typeToProcessor.TryGetValue(typeof(T), out var processor);
        return (processor ?? throw new ArgumentException("Could not found a processor")) as MessageProcessor<T> ??
               throw new ArgumentException("Could not convert processor to a required type");
    }
}