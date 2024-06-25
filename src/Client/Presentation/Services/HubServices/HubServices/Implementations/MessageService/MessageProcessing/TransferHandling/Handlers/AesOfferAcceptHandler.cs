using Client.Application.Cryptography.KeyStorage;
using Ethachat.Client.ClientOnlyModels.Events;
using Ethachat.Client.Services.HubServices.CommonServices.CallbackExecutor;
using EthachatShared.Encryption;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.MessageProcessing.
    TransferHandling.Handlers;

public class AesOfferAcceptHandler(ICallbackExecutor callbackExecutor, IKeyStorage keyStorage)
    : ITransferHandler<EventMessage>
{
    public async Task HandleAsync(EventMessage accept)
    {
        var keys = await keyStorage.GetAsync(accept.Sender, KeyType.Aes);
        var acceptedKey = keys.FirstOrDefault(x => x.Id == accept.Id) ?? throw new ArgumentException("Missing key");
        acceptedKey.IsAccepted = true;
        await keyStorage.UpdateAsync(acceptedKey);

        callbackExecutor.ExecuteSubscriptionsByName(accept.Sender, "OnPartnerAESKeyReady");
        callbackExecutor.ExecuteSubscriptionsByName(accept.Sender, "AESUpdated");
    }
}