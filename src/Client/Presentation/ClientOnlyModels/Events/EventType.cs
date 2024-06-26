namespace Ethachat.Client.ClientOnlyModels.Events;

public enum EventType
{
    Unset,
    OnTyping,
    MessageReceived,
    MessageRead,
    ResendRequest,
    ConversationDeletion,
    AesOfferAccepted,
    RsaPubKeyRequest
}