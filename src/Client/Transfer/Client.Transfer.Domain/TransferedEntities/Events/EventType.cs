namespace Client.Transfer.Domain.TransferedEntities.Events;

public enum EventType
{
    Unset,
    OnTyping,
    MessageReceived,
    MessageRead,
    ResendRequest,
    ConversationDeletion,
    AesOfferAccepted,
    RsaPubKeyRequest,
    DataTransferConfirmation
}