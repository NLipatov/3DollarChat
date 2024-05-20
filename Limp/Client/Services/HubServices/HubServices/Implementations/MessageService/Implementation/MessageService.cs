using System.Reflection;
using System.Text.Json;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Cryptography;
using Ethachat.Client.Services.AuthenticationService.Handlers;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Ethachat.Client.Services.InboxService;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Cryptography.Exceptions;
using Ethachat.Client.Services.ContactsProvider;
using Ethachat.Client.Services.HubServices.CommonServices.SubscriptionService;
using Ethachat.Client.Services.HubServices.HubServices.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Builders;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    AESTransmitting.Interface;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinaryReceiving;
using Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.
    BinarySending;
using Ethachat.Client.Services.KeyStorageService.Implementations;
using Ethachat.Client.Services.UndecryptedMessagesService;
using Ethachat.Client.Services.UndecryptedMessagesService.Models;
using Ethachat.Client.UI.Chat.Logic.MessageBuilder;
using EthachatShared.Constants;
using EthachatShared.Encryption;
using EthachatShared.Models.Authentication.Models;
using EthachatShared.Models.ConnectedUsersManaging;
using EthachatShared.Models.EventNameConstants;
using EthachatShared.Models.Message;
using EthachatShared.Models.Message.ClientToClientTransferData;
using EthachatShared.Models.Message.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using InMemoryKeyStorage = Ethachat.Client.Cryptography.KeyStorage.InMemoryKeyStorage;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        private NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly ICryptographyService _cryptographyService;
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private readonly IBinarySendingManager _binarySendingManager;
        private readonly IBinaryReceivingManager _binaryReceivingManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly IAesTransmissionManager _aesTransmissionManager;
        private readonly IContactsProvider _contactsProvider;
        private bool _isConnectionClosedCallbackSet = false;
        private AckMessageBuilder AckMessageBuilder => new();
        private HubConnection? HubConnection { get; set; }
        private LocalStorageKeyStorage KeyStorageService => new (_jsRuntime);

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            IUsersService usersService,
            ICallbackExecutor callbackExecutor,
            IMessageBuilder messageBuilder,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration,
            IBinarySendingManager binarySendingManager,
            IBinaryReceivingManager binaryReceivingManager,
            IJSRuntime jsRuntime,
            IAesTransmissionManager aesTransmissionManager,
            IContactsProvider contactsProvider, 
            IHubServiceSubscriptionManager hubServiceSubscriptionManager)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _messageBuilder = messageBuilder;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            _binarySendingManager = binarySendingManager;
            _binaryReceivingManager = binaryReceivingManager;
            _jsRuntime = jsRuntime;
            _aesTransmissionManager = aesTransmissionManager;
            _contactsProvider = contactsProvider;
            InitializeHubConnection();
            RegisterHubEventHandlers();

            _ = new UndecryptedMessagesStorageService(hubServiceSubscriptionManager, this, authenticationHandler);
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

            HubConnection.On<EncryptedDataTransfer>("OnTransfer", async edt =>
            {
                if (edt.Sender != await _authenticationHandler.GetUsernameAsync())
                {
                    var connection = await GetHubConnectionAsync();
                    await connection.SendAsync("OnTransferAcked", edt);
                }
                
                var decryptionMethodName = nameof(DecryptTransferAsync);
                MethodInfo? method = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == decryptionMethodName && m.IsGenericMethod);
                if (method != null)
                {
                    MethodInfo genericMethod = method.MakeGenericMethod(edt.DataType);
                    Task? task = (Task?)genericMethod.Invoke(this, [edt]);
                    if (task != null)
                    {
                        await task;
                        PropertyInfo? dataTypeProperty = task.GetType().GetProperty("Result");
                        if (dataTypeProperty != null)
                        {
                            var dataType = dataTypeProperty.GetValue(task, null);
                            if (edt.DataType == typeof(TextMessage))
                            {
                                var textMessage = (TextMessage?)dataType;
                                if (textMessage is null)
                                    throw new ArgumentException("Could not convert data transfer to target type");

                                await OnTextMessageReceived(textMessage);
                            }

                            if (edt.DataType == typeof(ClientMessage))
                            {
                                var clientMessage = (ClientMessage?)dataType;
                                if (clientMessage is null)
                                    throw new ArgumentException("Could not convert data transfer to target type");

                                if (clientMessage.Type is MessageType.ResendRequest)
                                {
                                    var message = _messageBox.Messages.FirstOrDefault(x => x.Id == clientMessage.Id);
                                    if (message?.Type is MessageType.TextMessage)
                                        await SendText(message);
                                }
                                
                                if (clientMessage.Type == MessageType.MessageReceivedConfirmation)
                                    _callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Id, "OnReceiverMarkedMessageAsReceived"); 
                                
                                if (clientMessage.Type == MessageType.MessageReadConfirmation)
                                    _callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Id, "OnReceiverMarkedMessageAsRead");

                                if (clientMessage.Type == MessageType.HLSPlaylist)
                                {
                                    _messageBox.AddMessage(clientMessage);
                                }

                                if (clientMessage.Type is MessageType.Metadata || clientMessage.Type is MessageType.DataPackage)
                                {
                                    _callbackExecutor.ExecuteSubscriptionsByName(clientMessage.Sender, "OnBinaryTransmitting");

                                    (bool isTransmissionCompleted, Guid fileId) progressStatus =
                                        await _binaryReceivingManager.StoreAsync(clientMessage);

                                    if (progressStatus.isTransmissionCompleted)
                                    {
                                        await NotifyAboutSuccessfullDataTransfer(progressStatus.fileId,
                                            clientMessage.Sender ?? throw new ArgumentException($"Invalid {clientMessage.Sender}"));
                                    }
                                }
                                
                                if (clientMessage.Type is MessageType.ConversationDeletionRequest)
                                {
                                    _messageBox.Delete(clientMessage.Sender);
                                }
                            }
                        }
                    }
                }
            });

            HubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            HubConnection.On<Guid>(SystemEventType.MessageRegisteredByHub.ToString(),
                messageId =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub");
                });

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
                        var aesKey = await KeyStorageService.GetLastAcceptedAsync(message.Sender, KeyType.Aes);
                        InMemoryKeyStorage.RSAKeyStorage.TryGetValue(message.Sender, out var rsaKey);
                        if (rsaKey?.Value?.ToString() == message.Cryptogramm?.Cyphertext &&
                            !string.IsNullOrWhiteSpace(rsaKey?.Value?.ToString()) &&
                            aesKey is not null)
                            return;

                        //Storing Public Key in our in-memory storage
                        InMemoryKeyStorage.RSAKeyStorage.TryAdd(message.Sender, new Key
                        {
                            Type = KeyType.RsaPublic,
                            Contact = message.Sender,
                            Format = KeyFormat.PemSpki,
                            Value = message.Cryptogramm!.Cyphertext
                        });
                        await RegenerateAESAsync(_cryptographyService, message.Sender, message.Cryptogramm.Cyphertext);
                    }

                    if (message.Type == MessageType.AesOfferAccept)
                    {
                        if (string.IsNullOrWhiteSpace(message.Sender)
                            || message.Cryptogramm?.KeyId == Guid.Empty
                            || message.Cryptogramm?.KeyId == null)
                            throw new ArgumentException("Invalid offer accept message");

                        await MarkKeyAsAccepted(message.Cryptogramm.KeyId, message.Sender);

                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "AESUpdated");
                        await MarkContactAsTrusted(message.Sender!);
                    }
                    else if (message.Type == MessageType.AesOffer)
                    {
                        var offerResponse = await _aesTransmissionManager.GenerateOfferResponse(message);
                        await MarkContactAsTrusted(message.Sender!);
                        await HubConnection.SendAsync("Dispatch", offerResponse);

                        if (offerResponse.Type is MessageType.AesOfferAccept)
                            _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "AESUpdated");
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

                if (string.IsNullOrWhiteSpace(InMemoryKeyStorage.MyRSAPublic?.Value?.ToString()))
                {
                    throw new ApplicationException("RSA Public key was not properly generated.");
                }

                await UpdateRSAPublicKeyAsync(InMemoryKeyStorage.MyRSAPublic);
            });

            HubConnection.On<Guid>("OnFileTransfered",
                messageId => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnFileReceived"); });

            HubConnection.On<string>("OnTyping",
                (partnerName) => { _callbackExecutor.ExecuteSubscriptionsByName(partnerName, "OnTyping"); });
        }

        private async Task MarkKeyAsAccepted(Guid keyId, string contact)
        {
            var keys = await KeyStorageService.GetAsync(contact, KeyType.Aes);
            var acceptedKeyId = keyId;

            var acceptedKey = keys.First(x => x.Id == acceptedKeyId);
            if (acceptedKey.IsAccepted)
                return;

            acceptedKey.IsAccepted = true;
            await KeyStorageService.UpdateAsync(acceptedKey);
        }

        private async Task NotifyAboutSuccessfullDataTransfer(Guid dataFileId, string sender)
        {
            if (HubConnection != null && HubConnection.State is HubConnectionState.Connected)
            {
                try
                {
                    await HubConnection.SendAsync("OnDataTranferSuccess", dataFileId, sender);
                }
                catch (Exception e)
                {
                    throw new ApplicationException
                        ($"{nameof(MessageService)}.{nameof(SendMessage)}: {e.Message}.");
                }
            }
            else
            {
                await GetHubConnectionAsync();
                await NotifyAboutSuccessfullDataTransfer(dataFileId, sender);
            }
        }

        public async Task UpdateRSAPublicKeyAsync(Key RSAPublicKey)
        {
            if (!InMemoryKeyStorage.isPublicKeySet)
            {
                await _usersService.SetRSAPublicKey(RSAPublicKey);
            }
        }

        private async Task RegenerateAESAsync
        (ICryptographyService cryptographyService,
            string partnersUsername,
            string partnersPublicKey)
        {
            var aesKey = await cryptographyService.GenerateAesKeyAsync(partnersUsername);

            var offer = await _aesTransmissionManager.GenerateOffer(partnersUsername, partnersPublicKey, aesKey);
            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("Dispatch", offer);
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            var connection = await GetHubConnectionAsync();

            await connection.SendAsync("GetAnRSAPublic", partnerUsername,
                await _authenticationHandler.GetUsernameAsync());
        }

        public async Task SendTypingEventToPartnerAsync(string sender, string receiver)
        {
            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("OnTyping", sender, receiver);
        }

        public async Task SendMessage(ClientMessage message)
        {
            switch (message.Type)
            {
                case MessageType.HLSPlaylist:
                    await SendHlsPlaylist(message);
                    break;
                case MessageType.TextMessage:
                    await SendText(message);
                    break;
                case MessageType.Metadata:
                    await _binarySendingManager.SendMetadata(message, GetHubConnectionAsync);
                    break;
                case MessageType.BrowserFileMessage:
                    await SendBrowserFile(message);
                    break;
                case MessageType.ResendRequest:
                    await TransferAsync(message);
                    break;
                case MessageType.ConversationDeletionRequest:
                    await TransferAsync(message);
                    break;
                default:
                    throw new ArgumentException($"Unhandled message type passed: {message.Type}.");
            }
        }

        private async Task SendHlsPlaylist(ClientMessage message)
        {
            AddToMessageBox(message);

            await TransferAsync(new ClientMessage
            {
                Id = message.Id,
                HlsPlaylist = message.HlsPlaylist,
                Target = message.Target,
                DateSent = DateTime.UtcNow,
                Type = MessageType.HLSPlaylist,
                Sender = await _authenticationHandler.GetUsernameAsync()
            });
        }

        private async Task OnTextMessageReceived(TextMessage textMessage)
        {
            if (textMessage.Total == 1 &&
                _messageBox.Contains(new Message { Id = textMessage.Id })) //not a composite message, duplicate
                return;

            await SendReceivedConfirmation(textMessage.Id, textMessage.Sender);

            _messageBox.AddMessage(textMessage);
        }

        private async Task SendText(ClientMessage message)
        {
            await AddToMessageBox(message.PlainText, message.Target, message.Id);
            await foreach (var tMessage in _messageBuilder.BuildTextMessage(message))
                await TransferAsync(tMessage, tMessage.Receiver
                                              ?? throw new ArgumentException("Target is a required parameter"));
        }

        private async Task SendBrowserFile(ClientMessage message)
        {
            await foreach (var dataPartMessage in _binarySendingManager.SendFile(message))
                await TransferAsync(dataPartMessage);
        }

        private async Task TransferAsync<T>(T data) where T: IIdentifiable, IDestinationResolvable
        {
            var serializedData = JsonSerializer.Serialize(data);

            var cryptogram = await _cryptographyService
                .EncryptAsync<AesHandler>(new Cryptogram
                {
                    Cyphertext = serializedData,
                }, contact: data.Target);

            var transferData = new EncryptedDataTransfer
            {
                Id = data.Id,
                Cryptogram = cryptogram,
                Target = data.Target,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                DataType = typeof(T)
            };

            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("TransferAsync", transferData);
        }
        
        

        private async Task TransferAsync<T>(T data, string target) where T: IIdentifiable
        {
            var serializedData = JsonSerializer.Serialize(data);

            var cryptogram = await _cryptographyService
                .EncryptAsync<AesHandler>(new Cryptogram
                {
                    Cyphertext = serializedData,
                }, contact: target);

            var transferData = new EncryptedDataTransfer
            {
                Id = data.Id,
                Cryptogram = cryptogram,
                Target = target,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                DataType = typeof(T)
            };

            var connection = await GetHubConnectionAsync();
            await connection.SendAsync("TransferAsync", transferData);
        }

        private async Task<T?> DecryptTransferAsync<T>(EncryptedDataTransfer dataTransfer)
        {
            try
            {
                var lastAcceptedAsync = await KeyStorageService.GetLastAcceptedAsync(dataTransfer.Sender, KeyType.Aes);
                if (lastAcceptedAsync is null)
                    await NegotiateOnAESAsync(dataTransfer.Sender);

                var cryptogram = await _cryptographyService
                    .DecryptAsync<AesHandler>(dataTransfer.Cryptogram, dataTransfer.Sender);

                var json = cryptogram.Cyphertext ?? string.Empty;
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception e)
            {
                if (e.InnerException is EncryptionKeyNotFoundException keyNotFoundException)
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(new UndecryptedItem
                    {
                        Id = dataTransfer.Id,
                        Sender = dataTransfer.Sender,
                        Target = dataTransfer.Target
                    },"OnDecryptionFailure");
                }
                Console.WriteLine(e);
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

        public async Task SendReceivedConfirmation(Guid messageId, string messageSender)
        {
            var myUsername = await _authenticationHandler.GetUsernameAsync();

            if (messageSender == myUsername)
                return;

            await TransferAsync(new ClientMessage()
                {
                    Id = messageId,
                    Sender = myUsername,
                    Target = messageSender,
                    Type = MessageType.MessageReceivedConfirmation
                });
        }

        public async Task NotifySenderThatMessageWasRead(Guid messageId, string messageSender, string myUsername)
        {
            if (messageSender == myUsername)
                return;
            
            await TransferAsync(new ClientMessage
            {
                Type = MessageType.MessageReadConfirmation,
                Target = messageSender,
                Sender = await _authenticationHandler.GetUsernameAsync(),
                Id = messageId  
            });
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