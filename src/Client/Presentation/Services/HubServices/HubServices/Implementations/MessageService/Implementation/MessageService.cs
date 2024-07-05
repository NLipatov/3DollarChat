using System.Reflection;
using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Client.Infrastructure.Cryptography.Handlers;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.Services.ContactsProvider;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing;
using
    Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling
    .Strategies.ReceiveStrategies;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.EventNameConstants;
using EthachatShared.Models.Message;
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
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IBinarySendingManager _binarySendingManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly IAesTransmissionManager _aesTransmissionManager;
        private readonly IContactsProvider _contactsProvider;
        private readonly IKeyStorage _keyStorage;
        private bool _isConnectionClosedCallbackSet = false;
        private AckMessageBuilder AckMessageBuilder => new();
        private HubConnection? HubConnection { get; set; }
        private MessageProcessor<KeyMessage> _keyMessageProcessor;
        private ITransferProcessorResolver _transferProcessorResolver;

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            IUsersService usersService,
            ICallbackExecutor callbackExecutor,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration,
            IBinaryReceivingManager binaryReceivingManager,
            IJSRuntime jsRuntime,
            IAesTransmissionManager aesTransmissionManager,
            IContactsProvider contactsProvider,
            IKeyStorage keyStorage)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            _binarySendingManager =
                new BinarySendingManager(jsRuntime, messageBox, callbackExecutor);
            _jsRuntime = jsRuntime;
            _aesTransmissionManager = aesTransmissionManager;
            _contactsProvider = contactsProvider;
            _keyStorage = keyStorage;
            InitializeHubConnection();
            RegisterHubEventHandlers();
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
                new AesOfferReceivedStrategy(_keyStorage, this, _callbackExecutor));

            keyMessageTransferReceivedHandlerFactory.RegisterHandler(KeyType.RsaPublic.ToString(),
                new RsaPubKeyMessageRequestReceivedStrategy(_keyStorage, _cryptographyService, _authenticationHandler,
                    this));
            keyMessageTransferReceivedHandlerFactory.RegisterHandler(KeyType.Aes.ToString(),
                new AesKeyMessageReceivedStrategy(_keyStorage, this, _callbackExecutor));

            _keyMessageProcessor = new(keyMessageTransferReceivedHandlerFactory);
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            //Shortcut connection is alive and ready to be used
            if (HubConnection?.State is HubConnectionState.Connected
                && !string.IsNullOrWhiteSpace(await _authenticationHandler.GetUsernameAsync()))
                return HubConnection;

            if (!await _authenticationHandler.IsSetToUseAsync())
            {
                NavigationManager.NavigateTo("signin");
                return null;
            }

            if (HubConnection == null)
                throw new ArgumentException($"{nameof(HubConnection)} was not properly instantiated.");

            while (HubConnection.State is HubConnectionState.Disconnected)
            {
                try
                {
                    await HubConnection.StartAsync();
                }
                catch
                {
                    var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                    await Task.Delay(interval);
                    return await GetHubConnectionAsync();
                }
            }

            await HubConnection.SendAsync("SetUsername", await _authenticationHandler.GetCredentialsDto());

            _callbackExecutor.ExecuteSubscriptionsByName(true, "OnMessageHubConnectionStatusChanged");

            if (_isConnectionClosedCallbackSet is false)
            {
                HubConnection.Closed += OnConnectionLost;
                _isConnectionClosedCallbackSet = true;
            }

            return HubConnection;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnMessageHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        private void InitializeHubConnection()
        {
            if (HubConnection is not null)
                return;

            HubConnection = HubServiceConnectionBuilder
                .Build(NavigationManager.ToAbsoluteUri(HubRelativeAddresses.MessageHubRelativeAddress));
        }

        private void RegisterHubEventHandlers()
        {
            if (HubConnection is null)
                throw new NullReferenceException("Could not register event handlers - hub was null.");

            HubConnection.On<EncryptedDataTransfer>("OnTransfer", async transfer =>
            {
                if (transfer.Sender != await _authenticationHandler.GetUsernameAsync())
                {
                    var connection = await GetHubConnectionAsync();
                    await connection.SendAsync("OnTransferAcked", transfer);
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

            HubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            HubConnection.On<Guid>(SystemEventType.MessageRegisteredByHub.ToString(),
                messageId => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub"); });

            HubConnection.On<Guid, int>("PackageRegisteredByHub", (fileId, packageIndex) =>
                _binarySendingManager.HandlePackageRegisteredByHub(fileId, packageIndex));

            HubConnection.On<AuthResult>("OnAccessTokenInvalid",
                authResult => { NavigationManager.NavigateTo("signin"); });

            HubConnection.On<Guid>("MetadataRegisteredByHub", metadataId => { });

            HubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    await (await GetHubConnectionAsync())
                        .SendAsync("OnAck", AckMessageBuilder.CreateMessageAck(message));

                    if (_messageBox.Contains(message))
                        return;

                    if (message.Type is MessageType.RsaPubKey)
                    {
                        await _keyStorage.StoreAsync(new Key
                        {
                            Type = KeyType.RsaPublic,
                            Contact = message.Sender,
                            Format = KeyFormat.PemSpki,
                            Value = message.Cryptogramm!.Cyphertext
                        });

                        await RegenerateAESAsync(_cryptographyService, message.Sender, message.Cryptogramm.Cyphertext);
                    }
                }
            });

            HubConnection.On<string>("OnMyNameResolved", async username =>
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

                await UpdateRSAPublicKeyAsync(rsaPublicKey);
            });
        }

        private async Task MarkKeyAsAccepted(Guid keyId, string contact)
        {
            var keys = await _keyStorage.GetAsync(contact, KeyType.Aes);
            var acceptedKeyId = keyId;

            var acceptedKey = keys.First(x => x.Id == acceptedKeyId);
            if (acceptedKey.IsAccepted)
                return;

            acceptedKey.IsAccepted = true;
            await _keyStorage.UpdateAsync(acceptedKey);
        }

        public async Task UpdateRSAPublicKeyAsync(Key RSAPublicKey)
        {
            await _usersService.SetRSAPublicKey(RSAPublicKey);
        }

        private async Task RegenerateAESAsync
        (ICryptographyService cryptographyService,
            string partnersUsername,
            string partnersPublicKey)
        {
            var aesKey = await cryptographyService.GenerateAesKeyAsync(partnersUsername);
            aesKey.Author = await _authenticationHandler.GetUsernameAsync();

            var offer = await _aesTransmissionManager.GenerateOffer(partnersUsername, aesKey);
            await TransferAsync(offer);
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            await (await GetHubConnectionAsync()).SendAsync("GetAnRSAPublic", partnerUsername,
                await _authenticationHandler.GetUsernameAsync());
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

        public async Task SendMessage(KeyMessage message)
        {
            await TransferAsync(message);
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
            };

            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("TransferAsync", transferData);
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
                (await _keyStorage.GetAsync(data.Target, KeyType.RsaPublic)).MaxBy(x => x.CreationDate);

            if (rsaKey is null)
                await (await GetHubConnectionAsync()).SendAsync("GetAnRSAPublic", data.Target,
                    await _authenticationHandler.GetUsernameAsync());

            return await _cryptographyService.EncryptAsync<RsaHandler, T>(data,
                rsaKey ?? throw new ApplicationException("Missing key"));
        }

        private async Task<T?> DecryptTransferAsync<T>(EncryptedDataTransfer dataTransfer)
        {
            try
            {
                if (dataTransfer.BinaryCryptogram.EncryptionKeyType is KeyType.RsaPublic)
                {
                    var rsaPrivateKey = (await _keyStorage.GetAsync(string.Empty, KeyType.RsaPrivate))
                        .OrderBy(x => x.CreationDate).FirstOrDefault();

                    var decryptedRsa =
                        await _cryptographyService.DecryptAsync<RsaHandler>(dataTransfer.BinaryCryptogram,
                            rsaPrivateKey);
                    var result = MessagePackSerializer.Deserialize<T>(decryptedRsa.Cypher);
                    return result;
                }

                if (dataTransfer.BinaryCryptogram.EncryptionKeyType is KeyType.Aes)
                {
                    var lastAcceptedAsync = await _keyStorage.GetLastAcceptedAsync(dataTransfer.Sender, KeyType.Aes);
                    if (lastAcceptedAsync is null)
                    {
                        await NegotiateOnAESAsync(dataTransfer.Sender);
                        throw new ApplicationException("Missing key");
                    }

                    var cryptogram = await _cryptographyService
                        .DecryptAsync<AesHandler>(dataTransfer.BinaryCryptogram, lastAcceptedAsync);

                    var result = MessagePackSerializer.Deserialize<T>(cryptogram.Cypher);
                    return result;
                }

                throw new ArgumentException();
            }
            catch (Exception e)
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

        private void AddToMessageBox(ClientMessage message)
        {
            _messageBox.AddMessage(message);
        }

        private async Task AddToMessageBox(string plainText, string target, Guid id)
        {
            var message = new ClientMessage
            {
                Id = id,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                Target = target,
                DateSent = DateTime.UtcNow,
                Type = MessageType.TextMessage
            };
            message.AddChunk(new()
            {
                Index = 0,
                Total = 1,
                Text = plainText
            });
            _messageBox.AddMessage(message);
        }


        private async Task MarkContactAsTrusted(string contactUsername)
        {
            var contact = await _contactsProvider.GetContact(contactUsername, _jsRuntime);
            if (contact is not null && !string.IsNullOrWhiteSpace(contact.TrustedPassphrase))
            {
                contact.IsTrusted = true;
                await _contactsProvider.UpdateContact(contact, _jsRuntime);
            }
        }
    }
}