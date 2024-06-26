using Client.Application.Cryptography;
using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.Services.AuthenticationService.Handlers;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class ReceivedRsaPubKeyMessageHandler(IKeyStorage keyStorage, ICryptographyService cryptographyService, IAuthenticationHandler authenticationHandler, IMessageService messageService) : ITransferHandler<KeyMessage>
{
    public async Task HandleAsync(KeyMessage message)
    {
        var aesKey = await cryptographyService.GenerateAesKeyAsync(message.Sender);
        aesKey.Author = await authenticationHandler.GetUsernameAsync();
        await keyStorage.StoreAsync(aesKey);

        await messageService.SendMessage(new KeyMessage
        {
            Key = aesKey,
            Target = message.Sender,
            Sender = message.Target
        });
    }
}