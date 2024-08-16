using System.Reflection;
using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Application.Gateway;
using Client.Infrastructure.Cryptography.Handlers;
using Client.Transfer.Domain.Entities.Events;
using Client.Transfer.Domain.Entities.Messages;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using
    Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Strategies.ReceiveStrategies;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.Cryptograms;
using EthachatShared.Models.EventNameConstants;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.DataTransfer;
using EthachatShared.Models.Message.Interfaces;
using EthachatShared.Models.Message.KeyTransmition;
using MessagePack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        private NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly ICryptographyService _cryptographyService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IBinarySendingManager _binarySendingManager;
        private readonly IKeyStorage _keyStorage;
        private readonly IGateway _gateway;
        private bool _isConnectionClosedCallbackSet = false;
        private ITransferProcessorResolver _transferProcessorResolver;

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            ICallbackExecutor callbackExecutor,
            IAuthenticationHandler authenticationHandler,
            IBinaryReceivingManager binaryReceivingManager,
            IJSRuntime jsRuntime,
            IKeyStorage keyStorage,
            IGateway gateway)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _callbackExecutor = callbackExecutor;
            _authenticationHandler = authenticationHandler;
            _binarySendingManager =
                new BinarySendingManager(jsRuntime, messageBox, callbackExecutor);
            _keyStorage = keyStorage;
            _gateway = gateway;
            RegisterTransferHandlers();
            _transferProcessorResolver = new TransferProcessorResolver(this, _callbackExecutor, _messageBox,
                _keyStorage, _authenticationHandler, _binarySendingManager, binaryReceivingManager,
                _cryptographyService);
        }

        private void RegisterTransferHandlers()
        {
            var aesOfferTransferReceivedHandlerFactory = new TransferHandlerFactory<AesOffer>();
            var keyMessageTransferReceivedHandlerFactory = new TransferHandlerFactory<KeyMessage>();

            aesOfferTransferReceivedHandlerFactory.RegisterHandler(nameof(AesOffer),
                new OnReceivedAesOffer(_keyStorage, this, _callbackExecutor));

            keyMessageTransferReceivedHandlerFactory.RegisterHandler(nameof(KeyMessage),
                new OnReceivedKeyMessage(_keyStorage, this, _cryptographyService));
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            await InitializeGatewayAsync();
            return null;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnMessageHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        private async Task InitializeGatewayAsync()
        {
            await _gateway.AuthenticateAsync(NavigationManager.ToAbsoluteUri(HubAddress.Message), await _authenticationHandler.GetCredentialsDto());

            await _gateway.AddEventCallbackAsync<EncryptedDataTransfer>("OnTransfer", async transfer =>
            {
                if (transfer.Sender != await _authenticationHandler.GetUsernameAsync())
                {
                    await _gateway.AckTransferAsync(transfer);
                }

                try
                {
                    var decryptionMethodName = nameof(DecryptTransferAsync);
                    MethodInfo? method = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == decryptionMethodName && m.IsGenericMethod);
                    if (method != null)
                    {
                        MethodInfo genericMethod = method.MakeGenericMethod(transfer.DataType);
                        Task? task = (Task?)genericMethod.Invoke(this, [transfer]);
                        if (task != null)
                        {
                            await task;
                            PropertyInfo? dataTypeProperty = task.GetType().GetProperty("Result");
                            if (dataTypeProperty != null)
                            {
                                var decryptedData = dataTypeProperty.GetValue(task, null);

                                MethodInfo? eventNameMethod = typeof(ITransferProcessorResolver)
                                    .GetMethod(nameof(ITransferProcessorResolver.GetEventName))?
                                    .MakeGenericMethod(transfer.DataType);

                                if (eventNameMethod == null)
                                    throw new ApplicationException("Could not resolve target method to invoke");

                                var eventName = (string?)eventNameMethod.Invoke(_transferProcessorResolver,
                                    new object[] { TransferDirection.Incoming });

                                if (eventName == null)
                                    throw new ApplicationException("Event name is null");

                                MethodInfo? getProcessorMethod = typeof(ITransferProcessorResolver)
                                    .GetMethod(nameof(ITransferProcessorResolver.GetProcessor))?
                                    .MakeGenericMethod(transfer.DataType);

                                if (getProcessorMethod == null)
                                    throw new ApplicationException("Could not resolve GetProcessor method");

                                var processor = getProcessorMethod.Invoke(_transferProcessorResolver, null);

                                if (processor == null)
                                    throw new ApplicationException("Processor is null");

                                MethodInfo? processTransferMethod =
                                    processor.GetType().GetMethod("ProcessTransferAsync");

                                if (processTransferMethod == null)
                                    throw new ApplicationException("Could not resolve ProcessTransferAsync method");

                                Task processTransferTask = (Task)processTransferMethod.Invoke(processor,
                                    new object[] { eventName, decryptedData });

                                if (processTransferTask is null)
                                    throw new ApplicationException("Could not resolve task to await");

                                await processTransferTask;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    await SendMessage(new EventMessage
                    {
                        Sender = await _authenticationHandler.GetUsernameAsync(),
                        Target = transfer.Sender,
                        Id = transfer.Id,
                        Type = EventType.ResendRequest
                    });
                }
            });

            await _gateway.AddEventCallbackAsync<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<Guid>(SystemEventType.MessageRegisteredByHub.ToString(),
                messageId =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<Guid, int>("PackageRegisteredByHub", (fileId, packageIndex) =>
            {
                _binarySendingManager.HandlePackageRegisteredByHub(fileId, packageIndex);
                return Task.CompletedTask;
            });

            await _gateway.AddEventCallbackAsync<AuthResult>("OnAccessTokenInvalid",
                authResult =>
                {
                    NavigationManager.NavigateTo("signin");
                    return Task.CompletedTask;
                });

            await _gateway.AddEventCallbackAsync<Guid>("MetadataRegisteredByHub", metadataId =>
            {
                return Task.CompletedTask;
            });

            await _gateway.AddEventCallbackAsync<string>("OnMyNameResolved", async username =>
            {
                if (!await _authenticationHandler.IsSetToUseAsync())
                {
                    NavigationManager.NavigateTo("/signIn");
                    return;
                }

                var rsaPublicKey = (await _keyStorage.GetAsync(string.Empty, KeyType.RsaPublic))
                    .OrderBy(x => x.CreationDate).First();
                if (string.IsNullOrWhiteSpace(rsaPublicKey.Value?.ToString()))
                {
                    throw new ApplicationException("RSA Public key was not properly generated.");
                }
            });
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            await UnsafeTransferAsync(new EncryptedDataTransfer
            {
                Id = Guid.NewGuid(),
                Sender = await _authenticationHandler.GetUsernameAsync(),
                Target = partnerUsername,
                DataType = typeof(EventMessage),
                BinaryCryptogram = new BinaryCryptogram
                {
                    EncryptionKeyType = KeyType.None,
                    Cypher = MessagePackSerializer.Serialize(new EventMessage
                    {
                        Id = Guid.NewGuid(),
                        Target = partnerUsername,
                        Sender = await _authenticationHandler.GetUsernameAsync(),
                        Type = EventType.RsaPubKeyRequest,
                    })
                }
            });
        }

        public async Task SendMessage<T>(T message) where T : IDestinationResolvable
        {
            var direction = message.Target == await _authenticationHandler.GetUsernameAsync()
                ? TransferDirection.Incoming
                : TransferDirection.Outcoming;

            var eventName = _transferProcessorResolver.GetEventName<T>(direction);
            var processor = _transferProcessorResolver.GetProcessor<T>();
            await processor.ProcessTransferAsync(eventName, message);
        }

        public async Task UnsafeTransferAsync(EncryptedDataTransfer data)
        {
            await _gateway.UnsafeTransferAsync(data);
        }

        public async Task TransferAsync<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable
        {
            var aesKey = await _keyStorage.GetLastAcceptedAsync(data.Target, KeyType.Aes);

            var encryptionTask = (aesKey is null) switch
            {
                true => RsaEncryptAsync(data),
                false => AesEncryptAsync(data)
            };

            var transferData = new EncryptedDataTransfer
            {
                Id = data.Id,
                BinaryCryptogram = await encryptionTask,
                Target = data.Target,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                DataType = typeof(T),
                IsPushRequired = IsWebPushRequired(data)
            };

            await _gateway.TransferAsync(transferData);
        }

        private bool IsWebPushRequired<T>(T data) where T : IIdentifiable, ISourceResolvable, IDestinationResolvable
        {
            return data switch
            {
                Package package => package.Index == 0, //if package with index 0 - true
                TextMessage textMessage => textMessage.Index == 0, //if text message with index 0 - true
                HlsPlaylistMessage => true,
                _ => false
            };
        }

        private async Task<BinaryCryptogram> AesEncryptAsync<T>(T data) where T : IIdentifiable, IDestinationResolvable
        {
            var aesKey = await _keyStorage.GetLastAcceptedAsync(data.Target, KeyType.Aes);

            return await _cryptographyService.EncryptAsync<AesHandler, T>(data,
                aesKey ?? throw new ApplicationException("Missing key"));
        }

        private async Task<BinaryCryptogram> RsaEncryptAsync<T>(T data)
            where T : IIdentifiable, ISourceResolvable, IDestinationResolvable
        {
            var rsaKey =
                (await _keyStorage.GetAsync(data.Target, KeyType.RsaPublic)).MaxBy(x => x.CreationDate) ??
                throw new NullReferenceException("Missing RSA key");

            return await _cryptographyService.EncryptAsync<RsaHandler, T>(data,
                rsaKey ?? throw new ApplicationException("Missing key"));
        }

        private async Task<T?> DecryptTransferAsync<T>(EncryptedDataTransfer dataTransfer)
        {
            try
            {
                if (dataTransfer.BinaryCryptogram.EncryptionKeyType is KeyType.None)
                {
                    var result = MessagePackSerializer.Deserialize<T>(dataTransfer.BinaryCryptogram.Cypher);
                    return result;
                }

                if (dataTransfer.BinaryCryptogram.EncryptionKeyType is KeyType.RsaPublic)
                {
                    var rsaPrivateKey = (await _keyStorage.GetAsync(string.Empty, KeyType.RsaPrivate))
                                        .MaxBy(x => x.CreationDate) ??
                                        throw new NullReferenceException("Missing key");

                    var decryptedRsa =
                        await _cryptographyService.DecryptAsync<RsaHandler>(dataTransfer.BinaryCryptogram,
                            rsaPrivateKey);
                    var result = MessagePackSerializer.Deserialize<T>(decryptedRsa.Cypher);
                    return result;
                }

                if (dataTransfer.BinaryCryptogram.EncryptionKeyType is KeyType.Aes)
                {
                    var aesKeys = await _keyStorage.GetAsync(dataTransfer.Sender, KeyType.Aes);
                    var aesKey = aesKeys.FirstOrDefault(x => x.Id == dataTransfer.BinaryCryptogram.KeyId) ??
                                 throw new NullReferenceException("Missing key");

                    var cryptogram = await _cryptographyService
                        .DecryptAsync<AesHandler>(dataTransfer.BinaryCryptogram, aesKey);

                    var result = MessagePackSerializer.Deserialize<T>(cryptogram.Cypher);
                    return result;
                }

                throw new ArgumentException();
            }
            catch (Exception)
            {
                await SendMessage(new EventMessage()
                {
                    Sender = await _authenticationHandler.GetUsernameAsync(),
                    Target = dataTransfer.Sender,
                    Id = dataTransfer.Id,
                    Type = EventType.ResendRequest
                });
                throw;
            }
        }
    }
}