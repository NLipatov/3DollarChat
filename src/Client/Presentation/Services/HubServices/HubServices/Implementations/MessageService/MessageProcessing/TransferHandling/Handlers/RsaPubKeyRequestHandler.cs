using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels;
using Ethachat.Client.ClientOnlyModels.Events;
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.TransferHandling.Handlers;

public class RsaPubKeyRequestHandler(IKeyStorage keyStorage, IMessageService messageService) : ITransferHandler<EventMessage>
{
    public async Task HandleAsync(EventMessage eventMessage)
    {
        var myRsaPublicKey = (await keyStorage.GetAsync(string.Empty, KeyType.RsaPublic)).MaxBy(x=>x.CreationDate);
        messageService.SendMessage(new KeyMessage
        {
            Sender = eventMessage.Target,
            Target = eventMessage.Sender,
            Key = myRsaPublicKey
        });
    }
}