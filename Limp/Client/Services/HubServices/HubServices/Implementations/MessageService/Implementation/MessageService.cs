using System.Collections.Concurrent;
using Limp.Client.ClientOnlyModels;
using Limp.Client.ClientOnlyModels.ClientOnlyExtentions;
using Limp.Client.Cryptography;
using Limp.Client.Cryptography.CryptoHandlers.Handlers;
using Limp.Client.Cryptography.KeyStorage;
using Limp.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling;
using Limp.Client.HubInteraction.Handlers.MessageDecryption;
using Limp.Client.Pages.Chat.Logic.MessageBuilder;
using Limp.Client.Services.AuthenticationService.Handlers;
using Limp.Client.Services.CloudKeyService;
using Limp.Client.Services.HubServices.CommonServices.CallbackExecutor;
using Limp.Client.Services.HubServices.HubServices.Implementations.UsersService;
using Limp.Client.Services.InboxService;
using LimpShared.Encryption;
using LimpShared.Models.Authentication.Types;
using LimpShared.Models.ConnectedUsersManaging;
using LimpShared.Models.Message;
using LimpShared.Models.Message.DataTransfer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Limp.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation
{
    public class MessageService : IMessageService
    {
        public NavigationManager NavigationManager { get; set; }
        private readonly IMessageBox _messageBox;
        private readonly ICryptographyService _cryptographyService;
        private readonly IAESOfferHandler _aESOfferHandler;
        private readonly IUsersService _usersService;
        private readonly ICallbackExecutor _callbackExecutor;
        private readonly IMessageBuilder _messageBuilder;
        private readonly IBrowserKeyStorage _browserKeyStorage;
        private readonly IMessageDecryptor _messageDecryptor;
        private readonly IAuthenticationHandler _authenticationHandler;
        private readonly IConfiguration _configuration;
        private string myName;
        private ConcurrentDictionary<Guid, List<Package>> ReceivedFileIdToPackages = new();
        private ConcurrentDictionary<Guid, List<Package>> SendedFileIdPackages = new();
        public bool IsConnected() => hubConnection?.State == HubConnectionState.Connected;

        private HubConnection? hubConnection { get; set; }

        public MessageService
        (IMessageBox messageBox,
            NavigationManager navigationManager,
            ICryptographyService cryptographyService,
            IAESOfferHandler aESOfferHandler,
            IUsersService usersService,
            ICallbackExecutor callbackExecutor,
            IMessageBuilder messageBuilder,
            IBrowserKeyStorage browserKeyStorage,
            IMessageDecryptor messageDecryptor,
            IAuthenticationHandler authenticationHandler,
            IConfiguration configuration)
        {
            _messageBox = messageBox;
            NavigationManager = navigationManager;
            _cryptographyService = cryptographyService;
            _aESOfferHandler = aESOfferHandler;
            _usersService = usersService;
            _callbackExecutor = callbackExecutor;
            _messageBuilder = messageBuilder;
            _browserKeyStorage = browserKeyStorage;
            _messageDecryptor = messageDecryptor;
            _authenticationHandler = authenticationHandler;
            _configuration = configuration;
            InitializeHubConnection();
            RegisterHubEventHandlers();
        }

        public async Task<HubConnection> GetHubConnectionAsync()
        {
            if (!await _authenticationHandler.IsSetToUseAsync())
            {
                NavigationManager.NavigateTo("signin");
                return null;
            }
            
            string? accessToken = await _authenticationHandler.GetAccessCredential();

            //Loading from local storage earlier saved AES keys
            InMemoryKeyStorage.AESKeyStorage =
                (await _browserKeyStorage.ReadLocalKeyChainAsync())?.AESKeyStorage ?? new();

            if (hubConnection == null)
                throw new ArgumentException($"{nameof(hubConnection)} was not properly instantiated.");

            while (hubConnection.State is not HubConnectionState.Connected)
            {
                try
                {
                    if (hubConnection.State is not HubConnectionState.Disconnected)
                        await hubConnection.StopAsync();

                    await hubConnection.StartAsync();
                }
                catch
                {
                    var interval = int.Parse(_configuration["HubConnection:ReconnectionIntervalMs"] ?? "0");
                    await Task.Delay(interval);
                    await GetHubConnectionAsync();
                    break;
                }
            }
            
            var authenticationType = await _authenticationHandler.GetAuthenticationTypeAsync();
            if (authenticationType is AuthenticationType.WebAuthn)
            {
                await hubConnection.SendAsync("SetUsername", null, await _authenticationHandler.GetCredentials());
            }
            else if (authenticationType is AuthenticationType.JwtToken)
            {
                await hubConnection.SendAsync("SetUsername", await _authenticationHandler.GetCredentials(), null);
            }
            else
            {
                throw new ArgumentException("Could not define used authentication mechanism.");
            }
            
            _callbackExecutor.ExecuteSubscriptionsByName(true, "OnMessageHubConnectionStatusChanged");

            hubConnection.Closed += OnConnectionLost;

            return hubConnection;
        }

        private async Task OnConnectionLost(Exception? exception)
        {
            _callbackExecutor.ExecuteSubscriptionsByName(false, "OnMessageHubConnectionStatusChanged");
            await GetHubConnectionAsync();
        }

        private void InitializeHubConnection()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/messageDispatcherHub"))
                .AddMessagePackProtocol()
                .Build();
        }

        private void RegisterHubEventHandlers()
        {
            if (hubConnection is null)
                throw new NullReferenceException($"Could not register event handlers - hub was null.");

            hubConnection.On<UserConnectionsReport>("ReceiveOnlineUsers",
                updatedTrackedUserConnections =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(updatedTrackedUserConnections, "ReceiveOnlineUsers");
                });

            hubConnection.On<Guid>("MessageRegisteredByHub",
                (messageId) => { _callbackExecutor.ExecuteSubscriptionsByName(messageId, "MessageRegisteredByHub"); });

            hubConnection.On<Guid, int>("PackageRegisteredByHub", (fileId, packageIndex) =>
            {
                Console.WriteLine($"package registered: {packageIndex}");
                _callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnChunkLoaded");

                SendedFileIdPackages.TryGetValue(fileId, out var sendedFilePackages);

                if (sendedFilePackages is null)
                    return;

                var updatedPackages = sendedFilePackages.Where(x => x.Index != packageIndex).ToList();
                SendedFileIdPackages.TryUpdate(fileId, updatedPackages, sendedFilePackages);

                if (!updatedPackages.Any())
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(fileId, "MessageRegisteredByHub");
                    SendedFileIdPackages.TryRemove(fileId, out updatedPackages);
                }
            });

            hubConnection.On<Message>("ReceiveMessage", async message =>
            {
                if (message.Sender != "You")
                {
                    if (hubConnection.State != HubConnectionState.Connected)
                    {
                        await hubConnection.StopAsync();
                        await hubConnection.StartAsync();
                    }

                    if (message.Type is MessageType.TextMessage)
                    {
                        if (string.IsNullOrWhiteSpace(message.Sender))
                            throw new ArgumentException(
                                $"Cannot get a message sender - {nameof(message.Sender)} contains empty string.");

                        Cryptogramm decryptedMessageCryptogramm = new Cryptogramm();
                        try
                        {
                            decryptedMessageCryptogramm = await _messageDecryptor.DecryptAsync(message);
                        }
                        catch (Exception ex)
                        {
                            await Console.Out.WriteLineAsync(
                                "Cannot decrypt received user message. Regenerating AES key.");
                            await NegotiateOnAESAsync(message.Sender);
                        }

                        if (!string.IsNullOrWhiteSpace(decryptedMessageCryptogramm.Cyphertext))
                        {
                            ClientMessage clientMessage = message.AsClientMessage();

                            if (!string.IsNullOrWhiteSpace(decryptedMessageCryptogramm.Cyphertext))
                                clientMessage.PlainText = decryptedMessageCryptogramm.Cyphertext;

                            await hubConnection.SendAsync("MessageReceived", message.Id, message.Sender);
                            await _messageBox.AddMessageAsync(clientMessage, false);
                        }
                        else
                        {
                            if (message.Sender is not null)
                                await NegotiateOnAESAsync(message.Sender);
                        }
                    }
                    else if (message.Type == MessageType.DataPackage)
                    {
                        Console.WriteLine($"Received package: {message.Package?.Index}");
                        var packagesExist = ReceivedFileIdToPackages.ContainsKey(message.Package.FileDataid);
                        if (!packagesExist)
                        {
                            ReceivedFileIdToPackages.TryAdd(message.Package.FileDataid,
                                new List<Package>(message.Package.Total));
                            ReceivedFileIdToPackages.TryGetValue(message.Package.FileDataid, out var savedPackages);
                        }

                        ReceivedFileIdToPackages[message.Package.FileDataid].Add(message.Package);
                        if (ReceivedFileIdToPackages[message.Package.FileDataid].Count == message.Package.Total)
                        {
                            await _messageBox.AddMessageAsync(new ClientMessage
                                {
                                    Packages = ReceivedFileIdToPackages[message.Package.FileDataid],
                                    Sender = message.Sender
                                },
                                isEncrypted: false);
                            ReceivedFileIdToPackages.TryRemove(message.Package.FileDataid, out _);
                            await NotifyAboutSuccessfullDataTransfer(message.Package.FileDataid, message.Sender);
                        }
                    }
                    else if (message.Type == MessageType.AesAccept)
                    {
                        _callbackExecutor.ExecuteSubscriptionsByName(message.Sender, "OnPartnerAESKeyReady");
                        _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        InMemoryKeyStorage.AESKeyStorage[message.Sender!].IsAccepted = true;
                        return;
                    }
                    else if (message.Type == MessageType.AesOffer)
                    {
                        if (hubConnection != null)
                        {
                            await hubConnection.SendAsync("Dispatch",
                                await _aESOfferHandler.GetAESOfferResponse(message));
                            _callbackExecutor.ExecuteSubscriptionsByName(true, "AESUpdated");
                        }
                    }
                }

                //If we dont yet know a partner Public Key and we dont have an AES Key for chat with partner,
                //we will request it from server side.
                if (InMemoryKeyStorage.RSAKeyStorage.FirstOrDefault(x => x.Key == message.Sender).Value == null
                    &&
                    _browserKeyStorage.GetAESKeyForChat(message.Sender!) == null)
                {
                    if (hubConnection == null)
                    {
                        await ReconnectAsync();
                    }
                    else
                        await hubConnection.SendAsync("GetAnRSAPublic", message.Sender);
                }
            });

            hubConnection.On<Guid>("OnReceiverMarkedMessageAsReceived",
                messageId =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsReceived");
                });

            hubConnection.On<Guid>("MessageHasBeenRead",
                messageId =>
                {
                    _callbackExecutor.ExecuteSubscriptionsByName(messageId, "OnReceiverMarkedMessageAsRead");
                });

            //Handling server side response on partners Public Key
            hubConnection.On<string, string>("ReceivePublicKey", async (partnersUsername, partnersPublicKey) =>
            {
                if (partnersUsername == "You")
                    return;
                //Storing Public Key in our in-memory storage
                InMemoryKeyStorage.RSAKeyStorage.TryAdd(partnersUsername, new Key
                {
                    Type = KeyType.RsaPublic,
                    Contact = partnersUsername,
                    Format = KeyFormat.PemSpki,
                    Value = partnersPublicKey
                });

                //Now we can send an encrypted offer on AES Key
                //We will encrypt our offer with a partners RSA Public Key
                await RegenerateAESAsync(_cryptographyService!, partnersUsername, partnersPublicKey);
            });

            hubConnection.On<string>("OnMyNameResolve", async username =>
            {
                myName = username;
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

            hubConnection.On<Guid>("OnFileTransfered", fileId =>
            {
                SendedFileIdPackages.TryRemove(fileId, out _);
                _callbackExecutor.ExecuteSubscriptionsByName(fileId, "OnFileReceived");
            });

            hubConnection.On<string>("OnConvertationDeleteRequest",
                partnerName => { _messageBox.Delete(partnerName); });
        }

        private async Task NotifyAboutSuccessfullDataTransfer(Guid fileId, string sender)
        {
            if (hubConnection != null && hubConnection.State is HubConnectionState.Connected)
            {
                try
                {
                    await hubConnection.SendAsync("OnDataTranferSuccess", fileId, sender);
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
                await NotifyAboutSuccessfullDataTransfer(fileId, sender);
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
            await cryptographyService.GenerateAesKeyAsync(partnersUsername, async (aesKeyForConversation) =>
            {
                if (hubConnection?.State is not HubConnectionState.Connected)
                {
                    await GetHubConnectionAsync();
                    while (hubConnection?.State is not HubConnectionState.Connected)
                    {
                        await GetHubConnectionAsync();
                    }
                }

                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.CreationDate =
                    DateTime.UtcNow;
                InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value =
                    aesKeyForConversation;
                await _browserKeyStorage.SaveInMemoryKeysInLocalStorage();
                string? offeredAESKeyForConversation =
                    InMemoryKeyStorage.AESKeyStorage.First(x => x.Key == partnersUsername).Value.Value!.ToString();

                if (string.IsNullOrWhiteSpace(offeredAESKeyForConversation))
                    throw new ApplicationException("Could not properly generated an AES Key for conversation");

                //When this callback is called, AES key for conversation is already generated
                //We now need to encrypt this AES key and send it to partner
                string? encryptedAESKey = (await cryptographyService
                    .EncryptAsync<RSAHandler>
                    (new Cryptogramm { Cyphertext = offeredAESKeyForConversation },
                        //We will encrypt it with partners Public Key, so he will be able to decrypt it with his Private Key
                        publicKeyToEncryptWith: partnersPublicKey)).Cyphertext;

                if (string.IsNullOrWhiteSpace(encryptedAESKey))
                    throw new ApplicationException("Could not encrypt a AES Key, got empty string.");

                Message offerOnAES = new()
                {
                    Type = MessageType.AesOffer,
                    DateSent = DateTime.UtcNow,
                    Sender = await _authenticationHandler.GetUsernameAsync(),
                    TargetGroup = partnersUsername,
                    Cryptogramm = new()
                    {
                        Cyphertext = encryptedAESKey,
                    }
                };

                if (hubConnection.State is not HubConnectionState.Connected)
                {
                    await GetHubConnectionAsync();
                }

                await hubConnection!.SendAsync("Dispatch", offerOnAES);
            });
        }

        public async Task NegotiateOnAESAsync(string partnerUsername)
        {
            if (hubConnection?.State is not HubConnectionState.Connected)
            {
                if (hubConnection is not null)
                    await hubConnection.StopAsync();

                await GetHubConnectionAsync();
                await NegotiateOnAESAsync(partnerUsername);
                return;
            }

            await hubConnection.SendAsync("GetAnRSAPublic", partnerUsername);
        }

        public async Task ReconnectAsync()
        {
            if (hubConnection is not null)
            {
                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
            }

            await GetHubConnectionAsync();
        }

        public async Task SendMessage(ClientMessage message)
        {
            hubConnection = await GetHubConnectionAsync();
            switch (message.Type)
            {
                case MessageType.TextMessage:
                    await SendText(message.PlainText, message.TargetGroup, myName);
                    break;
                case MessageType.DataPackage:
                    await SendData(message.Files, message.TargetGroup);
                    break;
                default:
                    throw new ArgumentException($"Unhandled message type passed: {message.Type}.");
            }
        }

        public async Task SendData(List<DataFile> files, string targetGroup)
        {
            hubConnection = await GetHubConnectionAsync();

            try
            {
                await AddDataToMessageBox(targetGroup, files);
                foreach (var file in files)
                {
                    SendedFileIdPackages.TryAdd(file.Id, file.Packages);

                    Parallel.For(0, file.Packages.Count,
                        async i => { await SendPackageMessage(i, file, targetGroup); });
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException($"{nameof(MessageService)}.{nameof(SendData)}: {e.Message}.");
            }
        }

        private async Task SendPackageMessage(int i, DataFile file, string targetGroup)
        {
            SendedFileIdPackages.TryGetValue(file.Id, out var packages);

            if (packages is null || packages.All(x => x.Index != i))
            {
                return;
            }

            var package = file.Packages[i];
            var message = new Message()
            {
                Id = file.Id,
                Package = package,
                Sender = myName,
                TargetGroup = targetGroup,
                Type = MessageType.DataPackage
            };

            var connection = new HubConnectionBuilder()
                .WithUrl(NavigationManager.ToAbsoluteUri("/messageDispatcherHub"))
                .AddMessagePackProtocol()
                .Build();

            await connection.StartAsync();
            await connection.SendAsync("Dispatch", message);
            Console.WriteLine($"Sent package: {message.Package.Index}");
            await connection.StopAsync();
            await connection.DisposeAsync();

            await Task.Delay(1000);

            await SendPackageMessage(i, file, targetGroup);
        }

        public async Task SendText(string text, string targetGroup, string myUsername)
        {
            Guid messageId = Guid.NewGuid();
            Message messageToSend =
                await _messageBuilder.BuildMessageToBeSend(text, targetGroup, myUsername, messageId);

            await AddToMessageBox(text, targetGroup, myUsername, messageId);

            if (hubConnection?.State is not HubConnectionState.Connected)
            {
                if (hubConnection is not null)
                    await hubConnection.StopAsync();

                await GetHubConnectionAsync();
                await SendText(text, targetGroup, myUsername);
                return;
            }

            await hubConnection.SendAsync("Dispatch", messageToSend);
        }

        public async Task RequestPartnerToDeleteConvertation(string targetGroup)
        {
            await GetHubConnectionAsync();
            await hubConnection!.SendAsync("DeleteConversation", myName, targetGroup);
        }

        public async Task AddDataToMessageBox(string targetGroup, List<DataFile> files)
        {
            var dataMessages = files.Select(x => new ClientMessage()
            {
                Packages = x.Packages,
                Id = x.Id,
                Sender = myName,
                TargetGroup = targetGroup,
                DateSent = DateTime.UtcNow,
                Type = MessageType.DataPackage
            }).ToList();

            await _messageBox.AddMessagesAsync(dataMessages.ToArray(), false);
        }

        private async Task AddToMessageBox(string text, string targetGroup, string myUsername, Guid messageId)
        {
            await _messageBox.AddMessageAsync(new ClientMessage
                {
                    Id = messageId,
                    Sender = myUsername,
                    TargetGroup = targetGroup,
                    PlainText = text,
                    DateSent = DateTime.UtcNow
                },
                isEncrypted: false);
        }

        public async Task NotifySenderThatMessageWasReaded(Guid messageId, string messageSender, string myUsername)
        {
            if (messageSender ==
                myUsername) //If it's our message, we don't want to notify partner that we've seen our message
                return;

            if (hubConnection is not null)
            {
                if (hubConnection?.State is not HubConnectionState.Connected)
                {
                    if (hubConnection is not null)
                        await hubConnection.StopAsync();

                    await GetHubConnectionAsync();
                    await NotifySenderThatMessageWasReaded(messageId, messageSender, myUsername);
                    return;
                }

                if (hubConnection.State is HubConnectionState.Connected)
                {
                    await hubConnection.SendAsync("MessageHasBeenRead", messageId, messageSender);
                }
            }

            throw new ArgumentException("Notification was not sent because hub connection is lost.");
        }
    }
}