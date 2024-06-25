using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class RsaPubKeyTextMessageHandler(IKeyStorage keyStorage) : ITransferHandler<KeyMessage>
{
    public async Task HandleAsync(KeyMessage message)
    {
        // await keyStorage.StoreAsync(message.Key);
        //
        // await RegenerateAESAsync(_cryptographyService, message.Sender, message.Cryptogramm.Cyphertext);
    }
}